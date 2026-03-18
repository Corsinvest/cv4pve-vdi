/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class AppColors
{
    // VM status
    public static readonly Color Running = Color.Parse("#22c55e");
    public static readonly Color Stopped = Color.Parse("#6b7280");
    public static readonly Color Shutdown = Color.Parse("#ef4444");
    public static readonly Color Windows = Color.Parse("#00adef");
    public static readonly Color Linux = Color.Parse("#f5c518");

    public static Color OsBrushColor(string osType)
        => osType.StartsWith("win")
            ? Windows
            : Linux;

    public static readonly Color TypeBadge = Color.Parse("#334155");

    // Progress bars ─
    public static readonly Color BarLow = Color.Parse("#22c55e");
    public static readonly Color BarMedium = Color.Parse("#f59e0b");
    public static readonly Color BarHigh = Color.Parse("#ef4444");

    // Stat chips in topbar
    public static readonly Color StatNodes = Color.Parse("#3b82f6");
    public static readonly Color StatVMs = Color.Parse("#8b5cf6");
    public static readonly Color StatCTs = Color.Parse("#f59e0b");

    public static readonly Color BorderDark = Color.Parse("#666666");
    public static readonly Color BorderLight = Color.Parse("#CCCCCC");

    public static readonly Color BusyOverlay = Color.FromArgb(160, 0, 0, 0);

    public static bool IsDark => Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    // Secondary text: gray that contrasts on both themes
    public static readonly Color SecondaryDark = Color.Parse("#BBBBBB");
    public static readonly Color SecondaryLight = Color.Parse("#666666");

    public static IBrush SecondaryBrush()
        => new SolidColorBrush(IsDark
            ? SecondaryDark
            : SecondaryLight);

    public static TextBlock Secondary(this TextBlock e)
    {
        e.Foreground = SecondaryBrush();
        return e;
    }

    // Helpers ─
    public static Color StatusColor(bool isActive)
        => isActive
            ? Running
            : Stopped;

    public static Color BarColor(double pct) =>
        pct > 80
            ? BarHigh
            : pct > 60
                ? BarMedium
                : BarLow;

    public static IBrush BorderBrush(bool isDark) =>
        new SolidColorBrush(isDark
            ? BorderDark
            : BorderLight);
}
