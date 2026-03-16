/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Reflection;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindowContext
{
    private static readonly string _version =
        typeof(MainWindowContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? typeof(MainWindowContext).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    private async Task ShowAboutAsync()
    {
        const string website = "https://corsinvest.it/cv4pve";
        const string repo = "https://github.com/Corsinvest/cv4pve-vdi";

        var dlg = new Window
        {
            Title = $"{L("About")} cv4pve-vdi",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(28, 24),
            Spacing = 16,
            Children =
            {
                // ── Header: logo + name + version ───────────────────────
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 14,
                    Children =
                    {
                        new PathIcon { Data = Geometry.Parse(Icons.Server), Width = 36, Height = 36 },
                        new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                new TextBlock { Text = "cv4pve-vdi", FontSize = 18, FontWeight = FontWeight.Bold },
                                new TextBlock { Text = $"v{_version}", FontSize = 11, Opacity = 0.55 }
                            }
                        }
                    }
                },

                new Border { Height = 1, Opacity = 0.12 },

                // ── Description ─────────────────────────────────────────
                new TextBlock
                {
                    Text = "VDI client for Proxmox VE.\nLaunches remote consoles via SPICE and RDP.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7,
                    FontSize = 12,
                    LineHeight = 20
                },

                new TextBlock
                {
                    Text = "© Corsinvest Srl — MIT License",
                    FontSize = 11,
                    Opacity = 0.45
                },

                new Border { Height = 1, Opacity = 0.12 },

                // ── Links ───────────────────────────────────────────────
                MakeLink(Icons.Globe,  "corsinvest.it/cv4pve", website),
                MakeLink(Icons.GitHub, "GitHub — Corsinvest/cv4pve-vdi", repo),

            }
        };

        dlg.Icon = MainWindowContext.AppIcon();
        await dlg.ShowDialog(_window!);
    }

    private static Control MakeLink(string iconData, string text, string url)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Cursor = new Cursor(StandardCursorType.Hand),
            Children =
            {
                new PathIcon { Data = Geometry.Parse(iconData), Width = 15, Height = 15, Opacity = 0.7 },
                new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextDecorations = TextDecorations.Underline,
                    Opacity = 0.8
                }
            }
        };
        panel.PointerPressed += (_, _) => OpenUrl(url);
        return panel;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }
}

// tiny fluent helper — avoids a separate extension class
internal static class ControlExtensions
{
    internal static T Also<T>(this T control, Action<T> configure) { configure(control); return control; }
}
