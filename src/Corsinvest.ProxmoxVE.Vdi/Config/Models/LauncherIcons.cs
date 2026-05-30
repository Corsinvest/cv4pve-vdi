/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

internal static class LauncherIcons
{
    public const string Rdp = "rdp";
    public const string Spice = "spice";
    public const string Vnc = "vnc";
    public const string Ssh = "ssh";
    public const string Ftp = "ftp";
    public const string Web = "web";
    public const string Database = "database";
    public const string Application = "application";

    public static readonly IReadOnlyList<string> All =
    [
        Application,
        Rdp,
        Spice,
        Vnc,
        Ssh,
        Ftp,
        Web,
        Database,
    ];
}
