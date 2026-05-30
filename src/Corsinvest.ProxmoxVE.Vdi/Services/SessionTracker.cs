/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

/// <summary>
/// In-memory list of viewer/launcher processes spawned by cv4pve-vdi.
/// Mirrors the Citrix SelfService → Connection Center pattern: the launcher
/// keeps the <see cref="Process"/> handles it spawned, so the "Open sessions"
/// panel binds to this list instead of enumerating foreign windows.
/// </summary>
internal sealed class SessionTracker
{
    public sealed record Entry(
        Process Process,
        long VmId,
        string VmName,
        string ServiceLabel,
        string IconData,
        string OsType,
        DateTime StartedAt);

    private readonly List<Entry> _sessions = [];

    /// <summary>Fires on add/remove. UI listens to redraw the sessions panel.</summary>
    public event Action? Changed;

    public IReadOnlyList<Entry> Sessions => _sessions;

    /// <summary>
    /// Adds the process to the list and wires its Exited event to auto-remove it.
    /// No-op (returns null) when the process is null or has already exited.
    /// </summary>
    public Entry? Register(Process? p, long vmId, string vmName, string serviceLabel, string iconData, string osType = "")
    {
        if (p is null) { return null; }
        try { if (p.HasExited) { return null; } } catch { return null; }

        var entry = new Entry(p, vmId, vmName, serviceLabel, iconData, osType, DateTime.Now);
        _sessions.Add(entry);

        try
        {
            p.EnableRaisingEvents = true;
            p.Exited += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                _sessions.Remove(entry);
                Changed?.Invoke();
            });
        }
        catch { /* if we can't subscribe, the entry simply stays until manual close */ }

        Changed?.Invoke();
        return entry;
    }

    /// <summary>Brings the viewer window to the foreground.</summary>
    public static void Activate(Entry e) => WindowActivator.BringToFront(e.Process);

    public static void Close(Entry e)
    {
        try
        {
            if (e.Process.HasExited) { return; }
            e.Process.CloseMainWindow();
            if (!e.Process.WaitForExit(1000)) { e.Process.Kill(); }
        }
        catch { /* best-effort */ }
    }

    public void CloseAll() => _sessions.ToList().ForEach(Close);
}
