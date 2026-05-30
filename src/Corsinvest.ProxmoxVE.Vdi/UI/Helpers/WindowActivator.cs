/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Runtime.Versioning;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

/// <summary>
/// Brings the main window of an external process to the foreground.
/// Used by the "Open sessions" panel to switch back to a running viewer
/// (remote-viewer, mstsc, putty, ...) when cv4pve-vdi runs as the Windows
/// shell in kiosk mode and there is no system taskbar.
/// </summary>
internal static class WindowActivator
{
    public static void BringToFront(Process process)
    {
        process.Refresh();
        if (OperatingSystem.IsWindows())    { Windows.Activate(process); }
        else if (OperatingSystem.IsMacOS()) { MacOS.Activate(process); }
        else if (OperatingSystem.IsLinux()) { Linux.Activate(process); }
    }

    // ---------------- WINDOWS ----------------
    [SupportedOSPlatform("windows")]
    private static class Windows
    {
        private const int SW_RESTORE = 9;

        public static void Activate(Process p)
        {
            // The user just clicked our pill, so cv4pve-vdi is already the
            // foreground process: a plain SetForegroundWindow is enough.
            // AttachThreadInput-style workarounds were tried but caused
            // system-wide input freezes on Windows 11.
            var hWnd = FindMainWindow(p.Id);
            if (hWnd == IntPtr.Zero) { return; }

            if (IsIconic(hWnd)) { ShowWindow(hWnd, SW_RESTORE); }
            SetForegroundWindow(hWnd);
        }

        private static IntPtr FindMainWindow(int pid)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint wPid);
                if (wPid == (uint)pid
                    && IsWindowVisible(hWnd)
                    && GetWindow(hWnd, GW_OWNER) == IntPtr.Zero) // top-level
                {
                    found = hWnd;
                    return false; // stop
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private const uint GW_OWNER = 4;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
    }

    // ---------------- macOS ----------------
    private static class MacOS
    {
        public static void Activate(Process p)
        {
            // Activate the process by its unix id (PID). On first run macOS
            // will ask for Accessibility/Automation permission.
            var script = $"tell application \"System Events\" to set frontmost of "
                       + $"(first process whose unix id is {p.Id}) to true";
            Run("osascript", $"-e '{script}'");
        }
    }

    // ---------------- LINUX ----------------
    private static class Linux
    {
        public static void Activate(Process p)
        {
            // X11: requires xdotool (or wmctrl). On Wayland, programmatic
            // activation of other applications' windows is blocked by design.
            Run("xdotool", $"search --pid {p.Id} windowactivate %@");
        }
    }

    private static void Run(string file, string args)
    {
        try
        {
            Process.Start(new ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { /* tool not installed / failure — silent */ }
    }
}
