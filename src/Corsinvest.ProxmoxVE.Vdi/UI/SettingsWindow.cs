/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    public static Window Create(AppConfig config, Action? onHostsChanged = null, int initialTab = 0, bool clustersOnly = false)
    {
        var btnSave = UiHelper.IconButton(AppIcons.Save, "Save");

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8),
            Spacing = 8
        };
        btnRow.Children.Add(btnSave);

        var dock = new DockPanel();
        DockPanel.SetDock(btnRow, Dock.Bottom);
        dock.Children.Add(btnRow);

        var tabControl = new TabControl { Margin = new Thickness(12, 12, 12, 0) };

        var window = new Window
        {
            Title = L("SettingsWindowTitle"),
            MinWidth = 500,
            MinHeight = 300,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = dock
        };

        var (tabAppearance, saveAppearance) = BuildTabAppearance(config);
        var (tabLaunchers, saveLaunchers) = BuildTabLaunchers(config, window);
        var (tabClusters, _) = BuildTabClusters(config, window, onHostsChanged);
        var (tabKiosk, saveKiosk) = BuildTabKiosk(config, window);

        // In kiosk mode, only the admin-unlocked session sees Launchers/Clusters/advanced Appearance
        var kioskLocked = config.Kiosk && !KioskGuard.IsAdminUnlocked;

        if (clustersOnly)
        {
            tabControl.Items.Add(tabClusters);
        }
        else
        {
            tabControl.Items.Add(tabAppearance);
            if (!kioskLocked) { tabControl.Items.Add(tabLaunchers); }
            if (!kioskLocked) { tabControl.Items.Add(tabClusters); }
            tabControl.Items.Add(tabKiosk);
            tabControl.SelectedIndex = initialTab < tabControl.Items.Count ? initialTab : 0;
        }

        // When kiosk-locked, opening the Kiosk tab triggers the admin password prompt.
        // On success, the session is unlocked but Settings has to be closed and reopened
        // so the previously hidden tabs (Launchers, Clusters, advanced Appearance) become visible.
        if (kioskLocked)
        {
            object? previousTab = tabControl.SelectedItem;

            tabControl.SelectionChanged += async (_, _) =>
            {
                if (tabControl.SelectedItem != tabKiosk)
                {
                    previousTab = tabControl.SelectedItem;
                    return;
                }

                if (await KioskGuard.CheckAsync(window, config))
                {
                    // Signal the caller that Settings should be reopened so the previously
                    // hidden tabs (Launchers, Clusters, advanced Appearance) become visible.
                    window.Tag = "reopen";
                    window.Close();
                }
                else
                {
                    tabControl.SelectedItem = previousTab;
                }
            };
        }

        dock.Children.Add(tabControl);

        btnSave.Click += async (_, _) =>
        {
            var kioskError = saveKiosk();
            if (kioskError != null)
            {
                await DialogHelper.MessageAsync(window, kioskError, NotificationSeverity.Error);
                return;
            }

            saveAppearance();
            if (!kioskLocked) { saveLaunchers(); }
            AppConfigManager.Save(config);
            Application.Current?.RequestedThemeVariant = config.ThemeVariant;
            window.Close();
        };

        return window;
    }

    // Section header helper shared across partial files
    internal static StackPanel SectionHeader(string label) => new()
    {
        Spacing = 4,
        Margin = new Thickness(0, 4, 0, 0),
        Children =
        {
            new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128)),
                LetterSpacing = 1.2
            },
            new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
            }
        }
    };
}
