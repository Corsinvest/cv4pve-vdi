/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private async Task ShowAboutAsync()
    {
        var dlg = new Window
        {
            Title = $"{L("About")} cv4pve-vdi",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(28, 24),
                Spacing = 16,
                Children =
                {
                    // Header: logo + name + version
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 14,
                        Children =
                        {
                            new PathIcon
                            {
                                Data = Geometry.Parse(AppIcons.Server),
                                Width = 36,
                                Height = 36
                            },
                            new StackPanel
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "cv4pve-vdi",
                                        FontSize = 18,
                                        FontWeight = FontWeight.Bold
                                    },
                                    new TextBlock
                                    {
                                        Text = $"v{ApplicationHelper.Version}",
                                        FontSize = 11,
                                        Opacity = 0.55
                                    }
                                }
                            }
                        }
                    },

                    new Border
                    {
                        Height = 1,
                        Opacity = 0.12
                    },

                    // Description
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

                    new Border
                    {
                        Height = 1,
                        Opacity = 0.12
                    },

                    // Links
                    MakeLink(AppIcons.Globe,  "corsinvest.it/cv4pve", "https://corsinvest.it/cv4pve"),
                    MakeLink(AppIcons.GitHub, "GitHub — Corsinvest/cv4pve-vdi", "https://github.com/Corsinvest/cv4pve-vdi"),
                    MakeLink(AppIcons.Account, "🚀 Who is using cv4pve-vdi?", "https://github.com/Corsinvest/cv4pve-vdi/issues/7"),

                }
            },

            Icon = AppIcon()
        };
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
                new PathIcon
                {
                    Data = Geometry.Parse(iconData),
                    Width = 15,
                    Height = 15,
                    Opacity = 0.7
                },
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
