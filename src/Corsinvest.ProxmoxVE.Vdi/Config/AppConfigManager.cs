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
            try { File.Copy(ConfigFile, $"{ConfigFile}.bak-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: false); } catch { }
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);

        // Write to a temp file in the same directory, then move over the target:
        // a crash mid-write must not leave a truncated config behind.
        var tmpFile = $"{ConfigFile}.tmp";
        File.WriteAllText(tmpFile, Serializer.Serialize(config));
        File.Move(tmpFile, ConfigFile, overwrite: true);

        // chmod 600 on Linux/macOS
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
