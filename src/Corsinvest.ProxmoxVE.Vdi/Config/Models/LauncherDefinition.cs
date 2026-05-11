/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

/// <summary>
/// YAML model for a launcher definition.
/// Each launcher targets a single platform.
/// </summary>
internal sealed class LauncherDefinition
{
    public string ServiceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int DefaultPort { get; set; }
    public bool SupportsCredentials { get; set; }
    public WindowsCredentialDefinition WindowsCredential { get; set; } = new();
    public string DocumentationUrl { get; set; } = string.Empty;
    public LauncherPlatform Platform { get; set; }
    public string Executable { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string ExtraArgs { get; set; } = string.Empty;
}
