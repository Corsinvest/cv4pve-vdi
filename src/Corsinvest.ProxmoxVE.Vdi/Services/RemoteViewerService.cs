/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using System.Diagnostics;
using System.Text;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class RemoteViewerService
{
    // Window during which a viewer exit counts as an immediate failure worth
    // reporting; a working session never ends this fast.
    private const int EarlyExitSeconds = 5;
    private const int MaxCapturedStderrChars = 2000;
    private const int MaxStderrTailChars = 800;

    /// <summary>
    /// Validates the configured viewer executable. Returns an error message, or
    /// null when the path looks usable. An empty path is valid here — "not
    /// configured" is reported separately.
    /// </summary>
    public static string? ValidateViewerPath(string? viewerPath)
    {
        if (string.IsNullOrWhiteSpace(viewerPath)) { return null; }

        if (!File.Exists(viewerPath))
        {
            return $"Viewer executable not found: {viewerPath}";
        }

        // virt-viewer treats a .vv file path as a local libvirt domain name and
        // never contacts Proxmox VE; the user sees the misleading
        // "Failed to connect: No running virtual machine found" dialog.
        var name = System.IO.Path.GetFileNameWithoutExtension(viewerPath);
        if (string.Equals(name, "virt-viewer", StringComparison.OrdinalIgnoreCase))
        {
            return $"{viewerPath} is virt-viewer, which cannot open .vv connection files: it looks " +
                   "for a local libvirt domain named after the file and then fails with " +
                   "\"Failed to connect: No running virtual machine found\". " +
                   "Use the remote-viewer executable instead.";
        }

        return null;
    }

    public static async Task<(string Error, Process? Process)>
        LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, ClusterConfig host,
                         Action<string>? onEarlyExit = null)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return ("SPICE viewer path is not configured. Please set it in Settings → Viewer.", null);
        }

        var validationError = ValidateViewerPath(config.ViewerPath);
        if (validationError != null) { return (validationError, null); }

        var (error, fileName) = await RemoteViewerHelper.PrepareSpiceAsync(client, node, vmType, vmId, host.Spice.Proxy);
        if (error != null) { return (error, null); }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        var p = LaunchViewer(config.ViewerPath, fileName!, viewerOptions, onEarlyExit);
        if (p == null) { return ("Failed to start viewer process.", null); }
        return (string.Empty, p);
    }

    public static async Task<(string Error, Process? Process)>
        LaunchVncAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config,
                       Action<string>? onEarlyExit = null)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return ("SPICE viewer path is not configured. Please set it in Settings → Viewer.", null);
        }

        var validationError = ValidateViewerPath(config.ViewerPath);
        if (validationError != null) { return (validationError, null); }

        var (error, fileName, bridge) = await RemoteViewerHelper.PrepareVncAsync(client, node, vmType, vmId);
        if (error != null) { return (error, null); }

        // VNC needs the WebSocket bridge to stay alive until the viewer exits,
        // so we launch the process here and dispose the bridge on the Exited event.
        var process = LaunchViewer(config.ViewerPath, fileName!, string.Empty, onEarlyExit);
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
    /// When <paramref name="onEarlyExit"/> is set, stderr is captured and the
    /// callback fires with the viewer's own failure reason if the process exits
    /// immediately — otherwise a broken viewer dies silently on the client side.
    /// </summary>
    private static Process? LaunchViewer(string viewerPath, string vvFile, string viewerOptions, Action<string>? onEarlyExit = null)
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

        if (onEarlyExit != null) { psi.RedirectStandardError = true; }

        Process? p;
        try { p = Process.Start(psi); }
        catch { return null; }

        if (p != null && onEarlyExit != null)
        {
            var stderr = new StringBuilder();
            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null || stderr.Length >= MaxCapturedStderrChars) { return; }
                stderr.AppendLine(e.Data);
            };
            p.BeginErrorReadLine();

            var startedAt = DateTime.UtcNow;
            p.EnableRaisingEvents = true;
            p.Exited += (_, _) =>
            {
                if ((DateTime.UtcNow - startedAt).TotalSeconds < EarlyExitSeconds)
                {
                    onEarlyExit(BuildEarlyExitMessage(viewerPath, stderr.ToString(), p.ExitCode));
                }
            };
        }

        return p;
    }

    private static string BuildEarlyExitMessage(string viewerPath, string stderr, int exitCode)
    {
        var tail = stderr.TrimEnd();
        if (tail.Length > MaxStderrTailChars) { tail = "…" + tail[^MaxStderrTailChars..]; }
        return tail.Length > 0
            ? $"The viewer {System.IO.Path.GetFileName(viewerPath)} exited immediately (exit code {exitCode}): {tail}"
            : $"The viewer {System.IO.Path.GetFileName(viewerPath)} exited immediately (exit code {exitCode}).";
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
