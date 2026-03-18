/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config;

internal class VdiConfig
{
    /// <summary>
    /// Last username used at login (pre-populated in login form)
    /// </summary>
    public string LastUser { get; set; } = string.Empty;

    /// <summary>
    /// Path to remote-viewer / virt-viewer executable for SPICE
    /// </summary>
    public string ViewerPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to RDP client executable (mstsc.exe on Windows, xfreerdp on Linux/Mac)
    /// Leave empty to use the system default
    /// </summary>
    public string RdpPath { get; set; } = string.Empty;

    /// <summary>
    /// UI theme: System, Light, Dark
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// List of PVE clusters/hosts
    /// </summary>
    public List<VdiHost> Hosts { get; set; } = [];

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
}
