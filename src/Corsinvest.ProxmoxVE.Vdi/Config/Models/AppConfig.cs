/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using YamlDotNet.Serialization;

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

internal class AppConfig
{
    public const string ThemeSystem = "System";
    public const string ThemeLight = "Light";
    public const string ThemeDark = "Dark";

    public const string ViewCard = "Card";
    public const string ViewList = "List";

    /// <summary>Default view: Card or List</summary>
    public string DefaultView { get; set; } = ViewCard;

    [YamlIgnore]
    public ThemeVariant ThemeVariant
        => Theme switch
        {
            ThemeLight => ThemeVariant.Light,
            ThemeDark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

    /// <summary>
    /// Last username used at login (pre-populated in login form)
    /// </summary>
    public string LastUser { get; set; } = string.Empty;

    /// <summary>
    /// Path to remote-viewer / virt-viewer executable for SPICE
    /// </summary>
    public string ViewerPath { get; set; } = string.Empty;

    /// <summary>
    /// UI theme: System, Light, Dark
    /// </summary>
    public string Theme { get; set; } = ThemeDark;

    /// <summary>Legacy field — migrates "hosts" from config files older than v1.3.0.</summary>
    public List<ClusterConfig> Hosts { get; set; } = [];
    public List<ClusterConfig> Clusters { get; set; } = [];

    // Appearance

    /// <summary>Show Start button in the Actions column (default false)</summary>
    public bool ShowStartButton { get; set; } = false;

    /// <summary>Show Shutdown button in the Actions column (default false)</summary>
    public bool ShowShutdownButton { get; set; } = false;

    /// <summary>Ask confirmation before starting a VM (default false)</summary>
    public bool ConfirmStart { get; set; } = false;

    /// <summary>Ask confirmation before shutting down a VM (default false)</summary>
    public bool ConfirmShutdown { get; set; } = false;

    /// <summary>Show CPU/RAM progress bars in card and list views (default true)</summary>
    public bool ShowBars { get; set; } = true;

    /// <summary>Show node filter section in sidebar (default false)</summary>
    public bool ShowNodes { get; set; } = false;

    /// <summary>Show pool filter in sidebar and pool info in card/list (default false)</summary>
    public bool ShowPools { get; set; } = false;

    /// <summary>Show tag badges in card/list and tag filter in sidebar (default true)</summary>
    public bool ShowTags { get; set; } = true;

    /// <summary>Enable SPICE console (default true)</summary>
    public bool EnableSpice { get; set; } = true;

    /// <summary>Enable VNC console (default true)</summary>
    public bool EnableVnc { get; set; } = true;

    /// <summary>Ping QEMU guest agent to detect if running (default false)</summary>
    public bool EnableAgentPing { get; set; } = false;

    // Kiosk

    /// <summary>
    /// Enable kiosk mode: window forced full-screen; access to Settings and closing the window require the admin password.
    /// </summary>
    public bool Kiosk { get; set; } = false;

    /// <summary>
    /// PBKDF2 hash (with salt) of the kiosk admin password. Empty = no password set.
    /// Format: <c>iterations.saltBase64.hashBase64</c>.
    /// </summary>
    public string KioskAdminPasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Optional background image shown behind the login form when kiosk mode is enabled.
    /// Empty = no background image.
    /// </summary>
    public string KioskLoginBackground { get; set; } = string.Empty;

    /// <summary>
    /// Force the application windows (Login + Main) to open in full-screen when kiosk mode is enabled.
    /// Set to <c>false</c> if shell-replacement / window manager already handles sizing externally.
    /// Default: <c>true</c>.
    /// </summary>
    public bool KioskForceFullScreen { get; set; } = true;
}
