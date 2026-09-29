/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SystemIO = System.IO;

namespace Corsinvest.ProxmoxVE.Vdi.Config;

internal static class AppConfigManager
{
    private static readonly string ConfigDir = SystemIO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "vdi");

    private static readonly string ConfigFile = SystemIO.Path.Combine(ConfigDir, "config");

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .WithAttributeOverride<AppConfig>(c => c.Hosts, new YamlIgnoreAttribute())
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static string LaunchersUserFile => SystemIO.Path.Combine(ConfigDir, "launchers.yaml");

    /// <summary>
    /// Copy of the configuration file that could not be read by <see cref="Load"/>, shown to the user
    /// at login; null when the file was read (or there was none).
    /// </summary>
    public static string? UnreadableConfigBackup { get; private set; }

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigFile)) { return new AppConfig(); }
        try
        {
            var config = Deserializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile)) ?? new AppConfig();

            // Migration: move legacy "hosts" to "clusters" (config files older than v1.3.0)
            if (config.Hosts.Count > 0 && config.Clusters.Count == 0)
            {
                config.Clusters = config.Hosts;
                config.Hosts = [];
            }

            return config;
        }
        catch
        {
            // Preserve the unreadable file instead of letting the next Save overwrite it
            // with a fresh default config.
            try
            {
                var backup = $"{ConfigFile}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
                WritePrivate(backup, File.ReadAllBytes(ConfigFile), FileMode.CreateNew);
                UnreadableConfigBackup = backup;
            }
            catch { }

            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);

        // Write to a temp file in the same directory, then move over the target:
        // a crash mid-write must not leave a truncated config behind.
        // The temp file is created 600, so the config takes that mode with the move.
        var tmpFile = $"{ConfigFile}.tmp";
        File.Delete(tmpFile);
        WritePrivate(tmpFile, System.Text.Encoding.UTF8.GetBytes(Serializer.Serialize(config)), FileMode.CreateNew);
        File.Move(tmpFile, ConfigFile, overwrite: true);

        // A readable config is on disk again: the login notice about the backup is no longer needed.
        UnreadableConfigBackup = null;
    }

    /// <summary>
    /// Writes a file readable only by the user (600 on Linux/macOS) from the moment it is created:
    /// the configuration holds service credentials, so no copy of it may be world-readable, even briefly.
    /// </summary>
    private static void WritePrivate(string path, byte[] content, FileMode mode)
    {
        var options = new FileStreamOptions
        {
            Mode = mode,
            Access = FileAccess.Write
        };

        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using var stream = new FileStream(path, options);
        stream.Write(content);
    }
}
