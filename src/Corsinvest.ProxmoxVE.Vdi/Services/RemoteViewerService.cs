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
    /// <summary>
    /// Startup banner warning for the configured viewer: null when the path is
    /// usable, otherwise a localized message ("not configured" or virt-viewer
    /// selected without a repairable remote-viewer sibling).
    /// </summary>
    public static string? GetViewerPathWarning(string? viewerPath)
        => string.IsNullOrWhiteSpace(viewerPath)
                ? L("ViewerNotConfigured")
                : ResolveRemoteViewerPath(viewerPath) is null
                        ? L("ViewerVirtViewerNeedsRemote")
                        : null;

    /// <summary>
    /// The Linux <c>virt-viewer</c> package and the Windows VirtViewer installer
    /// both place <c>remote-viewer</c> next to <c>virt-viewer</c>, so a path
    /// pointing at virt-viewer can be repaired on the fly. virt-viewer itself
    /// treats a .vv file as a local libvirt domain name and fails with the
    /// misleading "No running virtual machine found" dialog, so it must never
    /// be launched. Returns the usable path, or null when virt-viewer was
    /// selected and no remote-viewer sibling exists.
    /// </summary>
    public static string? ResolveRemoteViewerPath(string viewerPath)
    {
        if (!IsVirtViewer(viewerPath)) { return viewerPath; }

        var dir = System.IO.Path.GetDirectoryName(viewerPath);
        var sibling = string.IsNullOrEmpty(dir)
                            ? RemoteViewerName()
                            : System.IO.Path.Combine(dir, RemoteViewerName());

        // A bare file name resolves through PATH at launch — accept it as-is.
        return string.IsNullOrEmpty(dir) || File.Exists(sibling) ? sibling : null;
    }

    private static bool IsVirtViewer(string viewerPath)
        => string.Equals(System.IO.Path.GetFileName(viewerPath), VirtViewerName(), StringComparison.OrdinalIgnoreCase);

    private static string VirtViewerName() => OperatingSystem.IsWindows() ? "virt-viewer.exe" : "virt-viewer";

    private static string RemoteViewerName() => OperatingSystem.IsWindows() ? "remote-viewer.exe" : "remote-viewer";

    public static async Task<(string Error, Process? Process)>
        LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, ClusterConfig host)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return (L("ViewerPathNotSet"), null);
        }

        var viewerPath = ResolveRemoteViewerPath(config.ViewerPath);
        if (viewerPath is null) { return (L("ViewerVirtViewerNeedsRemote"), null); }

        var (error, fileName) = await RemoteViewerHelper.PrepareSpiceAsync(client, node, vmType, vmId, host.Spice.Proxy);
        if (error != null) { return (error, null); }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        var (p, launchError) = LaunchViewer(viewerPath, fileName!, viewerOptions);
        if (p == null) { return (launchError ?? L("ViewerLaunchFailed"), null); }
        return (string.Empty, p);
    }

    public static async Task<(string Error, Process? Process)>
        LaunchVncAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return (L("ViewerPathNotSet"), null);
        }

        var viewerPath = ResolveRemoteViewerPath(config.ViewerPath);
        if (viewerPath is null) { return (L("ViewerVirtViewerNeedsRemote"), null); }

        var (error, fileName, bridge) = await RemoteViewerHelper.PrepareVncAsync(client, node, vmType, vmId);
        if (error != null) { return (error, null); }

        // VNC needs the WebSocket bridge to stay alive until the viewer exits,
        // so we launch the process here and dispose the bridge on the Exited event.
        var (process, launchError) = LaunchViewer(viewerPath, fileName!, string.Empty);
        if (process == null)
        {
            await bridge!.DisposeAsync();
            return (launchError ?? L("ViewerLaunchFailed"), null);
        }

        process.EnableRaisingEvents = true;
        process.Exited += async (_, _) => { try { await bridge!.DisposeAsync(); } catch { } };

        return (string.Empty, process);
    }

    /// <summary>
    /// Launches the viewer binary directly (no shell wrapper) so we keep a usable
    /// <see cref="Process"/> handle for session tracking and bring-to-front.
    /// Returns the exception message as the error when the process could not be
    /// started (e.g. path does not exist), so the toast says why.
    /// </summary>
    private static (Process? Process, string? Error) LaunchViewer(string viewerPath, string vvFile, string viewerOptions)
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
        try { return (Process.Start(psi), null); }
        catch (Exception ex) { return (null, ex.Message); }
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
