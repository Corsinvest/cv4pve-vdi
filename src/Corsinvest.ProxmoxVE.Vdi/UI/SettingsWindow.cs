/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class SettingsWindow
{
    public static Window Create(VdiConfig config, Action? onHostsChanged = null, int initialTab = 0, bool clustersOnly = false)
    {
        // Tab 1: Appearance
        var themes = new[] { "System", "Light", "Dark" };
        var cmbTheme = new ComboBox
        {
            ItemsSource = themes,
            SelectedItem = themes.Contains(config.Theme)
                ? config.Theme
                : "System",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(26, 0, 0, 0)
        };
        var cmbThemeWithIcon = new Grid();
        cmbThemeWithIcon.Children.Add(cmbTheme);
        cmbThemeWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Palette));

        var chkShowBars = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.ChartBar, L("ShowBars")),
            IsChecked = config.ShowBars
        };
        var chkShowStart = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Play, L("ShowStartButton"), new SolidColorBrush(AppColors.Running)),
            IsChecked = config.ShowStartButton
        };
        var chkConfirmStart = new CheckBox
        {
            Content = L("AskConfirmation"),
            IsChecked = config.ConfirmStart,
            IsEnabled = config.ShowStartButton,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var chkShowShutdown = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Stop, L("ShowShutdownButton"), new SolidColorBrush(AppColors.Shutdown)),
            IsChecked = config.ShowShutdownButton
        };
        var chkConfirmShutdown = new CheckBox
        {
            Content = L("AskConfirmation"),
            IsChecked = config.ConfirmShutdown,
            IsEnabled = config.ShowShutdownButton,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Avalonia.Controls.Grid.SetColumn(chkConfirmStart, 1);
        Avalonia.Controls.Grid.SetColumn(chkConfirmShutdown, 1);

        chkShowStart.IsCheckedChanged += (_, _) => chkConfirmStart.IsEnabled = chkShowStart.IsChecked == true;
        chkShowShutdown.IsCheckedChanged += (_, _) => chkConfirmShutdown.IsEnabled = chkShowShutdown.IsChecked == true;

        var tabAppearance = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Monitor, L("TabAppearance")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = L("Theme"),
                        FontWeight = FontWeight.Bold
                    },
                    cmbThemeWithIcon,
                    chkShowBars,
                    AppIcons.WithText(AppIcons.Play, L("PowerButtons")),
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children = { chkShowStart, chkConfirmStart }
                    },
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children = { chkShowShutdown, chkConfirmShutdown }
                    },
                }
            }
        };

        // Tab 2: Viewer ─
        var txtViewerPath = new TextBox
        {
            Text = config.ViewerPath,
            Watermark = L("SpiceViewerWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Spice)
        };

        var btnBrowseViewer = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Folder),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(4, 0, 0, 0)
        };
        var viewerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        viewerRow.Add(txtViewerPath, 0);
        viewerRow.Add(btnBrowseViewer, 1);

        var txtRdpPath = new TextBox
        {
            Text = config.RdpPath,
            Watermark = L("RdpClientWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Rdp)
        };

        var btnBrowseRdp = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Folder),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(4, 0, 0, 0)
        };
        var rdpRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        rdpRow.Add(txtRdpPath, 0);
        rdpRow.Add(btnBrowseRdp, 1);

        var tabConnectivity = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Spice, L("TabViewer")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = L("SpiceViewerPath"),
                        FontWeight = FontWeight.Bold
                    },
                    viewerRow,
                    new TextBlock
                    {
                        Text = L("RdpClientPath"),
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 8, 0, 0)
                    },
                    rdpRow,
                }
            }
        };

        // Tab 3: Clusters
        var hostItems = new ObservableCollection<VdiHost>(config.Hosts);

        var lstHosts = new ListBox
        {
            ItemsSource = hostItems,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 160,
            ItemTemplate = new FuncDataTemplate<VdiHost>((h, _) =>
                new TextBlock
                {
                    Text = h == null
                            ? ""
                            : $"{h.Name}  ({h.Hosts})"
                })
        };

        var btnAddHost = new Button
        {
            Content = AppIcons.WithText(AppIcons.Add, L("Add")),
            Margin = new Thickness(0, 0, 4, 0)
        };
        var btnEditHost = new Button
        {
            Content = AppIcons.WithText(AppIcons.Edit, L("Edit")),
            Margin = new Thickness(0, 0, 4, 0),
            IsEnabled = false
        };
        var btnDelHost = new Button
        {
            Content = AppIcons.WithText(AppIcons.Delete, L("Delete")),
            IsEnabled = false
        };

        lstHosts.SelectionChanged += (_, _) =>
        {
            btnEditHost.IsEnabled = lstHosts.SelectedIndex >= 0;
            btnDelHost.IsEnabled = lstHosts.SelectedIndex >= 0;
        };

        var tabClusters = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Server, L("TabClusters")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    lstHosts,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children    = { btnAddHost, btnEditHost, btnDelHost }
                    }
                }
            }
        };

        var btnSave = new Button
        {
            Content = AppIcons.WithText(AppIcons.Save, L("Save"))
        };
        var btnCancel = new Button
        {
            Content = AppIcons.WithText(AppIcons.Close, L("Cancel"))
        };

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8),
            Spacing = 8
        };
        btnRow.Children.Add(btnCancel);
        btnRow.Children.Add(btnSave);

        var dock = new DockPanel();
        Avalonia.Controls.DockPanel.SetDock(btnRow, Dock.Bottom);
        dock.Children.Add(btnRow);
        var tabControl = new TabControl
        {
            Margin = new Thickness(12, 12, 12, 0),
        };
        if (clustersOnly)
        {
            tabControl.Items.Add(tabClusters);
        }
        else
        {
            tabControl.Items.Add(tabAppearance);
            tabControl.Items.Add(tabConnectivity);
            tabControl.Items.Add(tabClusters);
            tabControl.SelectedIndex = initialTab;
        }
        dock.Children.Add(tabControl);

        Window? window = null;
        window = new Window
        {
            Title = L("SettingsWindowTitle"),
            Width = 500,
            MinHeight = 300,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = dock
        };

        async Task BrowseFile(TextBox target, string titleKey)
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new()
            {
                Title = L(titleKey),
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                target.Text = files[0].Path.LocalPath;
            }
        }

        btnBrowseViewer.Click += async (_, _) => await BrowseFile(txtViewerPath, "SelectSpiceViewer");
        btnBrowseRdp.Click += async (_, _) => await BrowseFile(txtRdpPath, "SelectRdpClient");

        async Task EditHostAt(int idx)
        {
            if (idx < 0 || idx >= config.Hosts.Count)
            {
                return;
            }

            var dlg = HostEditWindow.Create(config.Hosts[idx]);
            dlg.Icon = MainWindow.AppIcon();
            var result = await dlg.ShowDialog<VdiHost?>(window);
            if (result != null)
            {
                config.Hosts[idx] = result;
                hostItems[idx] = result;
                VdiConfigManager.Save(config);
                onHostsChanged?.Invoke();
                lstHosts.SelectedIndex = idx;
            }
        }

        lstHosts.DoubleTapped += async (_, _) => await EditHostAt(lstHosts.SelectedIndex);
        btnEditHost.Click += async (_, _) => await EditHostAt(lstHosts.SelectedIndex);

        btnAddHost.Click += async (_, _) =>
        {
            var dlg = HostEditWindow.Create(null);
            dlg.Icon = MainWindow.AppIcon();
            var result = await dlg.ShowDialog<VdiHost?>(window);
            if (result != null)
            {
                config.Hosts.Add(result);
                hostItems.Add(result);
                VdiConfigManager.Save(config);
                onHostsChanged?.Invoke();
                lstHosts.SelectedIndex = hostItems.Count - 1;
            }
        };

        btnDelHost.Click += (_, _) =>
        {
            var idx = lstHosts.SelectedIndex;
            if (idx < 0 || idx >= config.Hosts.Count)
            {
                return;
            }

            config.Hosts.RemoveAt(idx);
            hostItems.RemoveAt(idx);
            VdiConfigManager.Save(config);
            onHostsChanged?.Invoke();
        };

        btnSave.Click += (_, _) =>
        {
            config.ViewerPath = txtViewerPath.Text ?? string.Empty;
            config.RdpPath = txtRdpPath.Text ?? string.Empty;
            config.Theme = cmbTheme.SelectedItem as string ?? "System";
            config.ShowBars = chkShowBars.IsChecked == true;
            config.ShowStartButton = chkShowStart.IsChecked == true;
            config.ShowShutdownButton = chkShowShutdown.IsChecked == true;
            config.ConfirmStart = chkConfirmStart.IsChecked == true;
            config.ConfirmShutdown = chkConfirmShutdown.IsChecked == true;
            VdiConfigManager.Save(config);

            var variant = config.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            Avalonia.Application.Current?.RequestedThemeVariant = variant;
            window.Close();
        };

        btnCancel.Click += (_, _) => window.Close();

        return window;
    }
}
