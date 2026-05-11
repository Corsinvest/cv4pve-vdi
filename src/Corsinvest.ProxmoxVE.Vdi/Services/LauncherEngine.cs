/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

/// <summary>
/// <para>
/// Loads launcher definitions from embedded YAML and optional user override file,
/// then executes them by interpolating argument templates.
/// </para>
/// <para>
/// Template tokens:
///   {ip}         — target IP address
///   {port}       — target port (always substituted)
///   {username}   — credential username
///   {password}   — credential password
///   {extraArgs}  — extra arguments from definition
///   {?TEXT}      — include TEXT (with inner tokens resolved) only if all inner tokens are non-empty
/// </para>
/// </summary>
internal static partial class LauncherEngine
{
    private const string EmbeddedResourceName = "Corsinvest.ProxmoxVE.Vdi.Config.launchers.yaml";

    private static readonly IDeserializer Deserializer
        = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    /// <summary>
    /// Returns all launchers supported on the current platform (non-hidden).
    /// </summary>
    public static IReadOnlyList<LauncherDefinition> LoadForCurrentPlatform(string? userYamlPath = null)
    {
        var definitions = LoadAll(userYamlPath);
        var currentPlatform = CurrentPlatform();
        return [.. definitions.Where(d => d.Platform == currentPlatform
                                          && !string.IsNullOrWhiteSpace(d.Executable))];
    }

    /// <summary>
    /// Returns all launchers (including hidden) merged with user overrides.
    /// Used by the Settings UI to show and edit the full list.
    /// </summary>
    public static IReadOnlyList<LauncherDefinition> LoadAll(string? userYamlPath = null)
    {
        var definitions = LoadEmbedded();

        if (!string.IsNullOrEmpty(userYamlPath) && File.Exists(userYamlPath))
        {
            foreach (var userDef in LoadFile(userYamlPath))
            {
                var idx = definitions.FindIndex(d => d.ServiceId == userDef.ServiceId);
                if (idx >= 0)
                {
                    definitions[idx] = Merge(definitions[idx], userDef);
                }
                else
                {
                    definitions.Add(userDef);
                }
            }
        }

        return definitions;
    }

    /// <summary>
    /// Saves the user launcher overrides to the given path.
    /// </summary>
    public static void SaveUserLaunchers(IEnumerable<LauncherDefinition> userLaunchers, string userYamlPath)
    {
        var serializer = new SerializerBuilder()
                            .WithNamingConvention(HyphenatedNamingConvention.Instance)
                            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
                            .Build();

        File.WriteAllText(userYamlPath, serializer.Serialize(new Dictionary<string, object> { ["launchers"] = userLaunchers.ToList() }));
    }

    /// <summary>
    /// Launches the given definition with the provided IP, port and credentials.
    /// Returns an error message, or empty string on success.
    /// </summary>
    public static string Launch(LauncherDefinition def, string ip, int port, Credentials? credentials, string? extraArgsOverride = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(def.Executable)) { return $"No executable defined for launcher '{def.ServiceId}'."; }

            var extraArgs = extraArgsOverride ?? def.ExtraArgs;
            var effectivePort = port > 0 ? port : def.DefaultPort;
            var args = Interpolate(def.Arguments, ip, effectivePort, def.DefaultPort, credentials, extraArgs);

            if (def.WindowsCredential.Enable && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"Launcher '{def.ServiceId}' requires Windows Credential Manager, which is not supported on this platform.";
            }

            if (def.WindowsCredential.Enable)
            {
                var target = def.WindowsCredential.Target.Replace("{ip}", ip);
                WindowsCredentialManager.WithTemporaryCredential(target,
                                                                 def.WindowsCredential.Type,
                                                                 credentials,
                                                                 () => Start(def.Executable, args));
            }
            else
            {
                Start(def.Executable, args);
            }

            return string.Empty;
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static List<LauncherDefinition> LoadEmbedded()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' not found.");
        using var reader = new StreamReader(stream);
        return ParseYaml(reader.ReadToEnd());
    }

    public static List<LauncherDefinition> LoadFile(string path)
    {
        try { return ParseYaml(File.ReadAllText(path)); }
        catch { return []; }
    }

    private static List<LauncherDefinition> ParseYaml(string yaml)
        => Deserializer.Deserialize<Dictionary<string, List<LauncherDefinition>>>(yaml)
                       .TryGetValue("launchers", out var list)
            ? list
            : [];

    public static LauncherDefinition MergeSingle(LauncherDefinition builtin, LauncherDefinition user)
        => Merge(builtin, user);

    /// <summary>Applies non-default values from <paramref name="user"/> onto <paramref name="builtin"/>.</summary>
    private static LauncherDefinition Merge(LauncherDefinition builtin, LauncherDefinition user)
        => new()
        {
            ServiceId = builtin.ServiceId,
            Platform = user.Platform,

            DisplayName = string.IsNullOrEmpty(user.DisplayName)
                            ? builtin.DisplayName
                            : user.DisplayName,

            DefaultPort = user.DefaultPort == 0
                            ? builtin.DefaultPort
                            : user.DefaultPort,

            SupportsCredentials = user.SupportsCredentials,
            WindowsCredential = user.WindowsCredential,

            DocumentationUrl = string.IsNullOrEmpty(user.DocumentationUrl)
                            ? builtin.DocumentationUrl
                            : user.DocumentationUrl,

            Arguments = string.IsNullOrEmpty(user.Arguments)
                        ? builtin.Arguments
                        : user.Arguments,

            ExtraArgs = string.IsNullOrEmpty(user.ExtraArgs)
                        ? builtin.ExtraArgs
                        : user.ExtraArgs,

            Executable = string.IsNullOrEmpty(user.Executable)
                        ? builtin.Executable
                        : user.Executable,
        };

    // Interpolation
    private static readonly Regex ConditionalPattern = ConditionalTokenRegex();

    private static string Interpolate(string template, string ip, int port, int defaultPort,
                                      Credentials? credentials, string extraArgs)
    {
        var username = credentials?.Username ?? string.Empty;
        var password = credentials?.Password ?? string.Empty;

        var portStr = port == defaultPort ? string.Empty : port.ToString();

        // Resolve {?...} conditionals first
        var result = ConditionalPattern.Replace(template, m =>
        {
            var inner = m.Groups[1].Value;
            var resolved = ResolveTokens(inner, ip, portStr, username, password, extraArgs);
            return HasEmptyToken(inner, portStr, username, password)
                    ? string.Empty
                    : resolved;
        });

        result = ResolveTokens(result, ip, portStr, username, password, extraArgs);

        // Collapse multiple spaces
        return MultipleSpacesRegex().Replace(result.Trim(), " ");
    }

    private static string ResolveTokens(string s, string ip, string port, string username, string password, string extraArgs)
        => s.Replace("{ip}", ip)
            .Replace("{port}", port)
            .Replace("{username}", username)
            .Replace("{password}", password)
            .Replace("{extraArgs}", extraArgs);

    private static bool HasEmptyToken(string inner, string port, string username, string password)
    {
        if (inner.Contains("{port}") && string.IsNullOrEmpty(port)) { return true; }
        if (inner.Contains("{username}") && string.IsNullOrEmpty(username)) { return true; }
        if (inner.Contains("{password}") && string.IsNullOrEmpty(password)) { return true; }
        return false;
    }

    private static void Start(string fileName, string arguments)
    {
        Console.WriteLine($"[LauncherEngine] {fileName} {arguments}");
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true
        });
    }

    private static LauncherPlatform CurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return LauncherPlatform.Windows; }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return LauncherPlatform.OSX; }
        return LauncherPlatform.Linux;
    }

    [GeneratedRegex(@"\{\?([^{}]*(?:\{[^}]*\}[^{}]*)*)\}")]
    private static partial Regex ConditionalTokenRegex();

    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpacesRegex();
}
