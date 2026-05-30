/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class RemoteViewerService
{
    public static async Task<(string Error, Process? Process)>
        LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, ClusterConfig host)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return ("SPICE viewer path is not configured. Please set it in Settings → Viewer.", null);
        }

        var (error, fileName) = await RemoteViewerHelper.PrepareSpiceAsync(client, node, vmType, vmId, host.Spice.Proxy);
        if (error != null) { return (error, null); }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        var p = LaunchViewer(config.ViewerPath, fileName!, viewerOptions);
        return (string.Empty, p);
    }

    public static async Task<(string Error, Process? Process)>
        LaunchVncAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return ("SPICE viewer path is not configured. Please set it in Settings → Viewer.", null);
        }

        var (error, fileName, bridge) = await RemoteViewerHelper.PrepareVncAsync(client, node, vmType, vmId);
        if (error != null) { return (error, null); }

        // VNC needs the WebSocket bridge to stay alive until the viewer exits,
        // so we launch the process here and dispose the bridge on the Exited event.
        var process = LaunchViewer(config.ViewerPath, fileName!, string.Empty);
        if (process == null)
        {
            await bridge!.DisposeAsync();
            return ("Failed to start viewer process.", null);
        }

        process.EnableRaisingEvents = true;
        process.Exited += async (_, _) => { try { await bridge!.DisposeAsync(); } catch { } };

        return (string.Empty, process);
    }

    /// <summary>
    /// Launches the viewer binary directly (no shell wrapper) so we keep a usable
    /// <see cref="Process"/> handle for session tracking and bring-to-front.
    /// </summary>
    private static Process? LaunchViewer(string viewerPath, string vvFile, string viewerOptions)
    {
        var psi = new ProcessStartInfo
        {
            FileName = viewerPath,
            UseShellExecute = false,
            // remote-viewer.exe on Windows is built as a console subsystem app
            // (it still draws its own GUI on top): without CreateNoWindow Windows
            // attaches an empty console to our process. If cv4pve-vdi then exits,
            // that orphaned console window stays on screen until the viewer dies.
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(vvFile);
        foreach (var opt in SplitArgs(viewerOptions))
        {
            psi.ArgumentList.Add(opt);
        }
        try { return Process.Start(psi); }
        catch { return null; }
    }

    /// <summary>
    /// Minimal whitespace-respecting split that honors double quotes so a single
    /// viewer option like <c>--title "My VM"</c> stays together.
    /// </summary>
    private static IEnumerable<string> SplitArgs(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) { yield break; }
        var sb = new System.Text.StringBuilder();
        var inQuote = false;
        foreach (var c in raw)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (char.IsWhiteSpace(c) && !inQuote)
            {
                if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
                continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) { yield return sb.ToString(); }
    }
}
