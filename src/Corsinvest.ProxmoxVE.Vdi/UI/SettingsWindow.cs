/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Platform.Storage;
using Corsinvest.ProxmoxVE.Vdi.Config;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class SettingsWindow
{
    public static Window Create(VdiConfig config, Action? onHostsChanged = null, int initialTab = 0)
    {
        // ── Tab 1: Appearance ─────────────────────────────────────────────
        var themes = new[] { "System", "Light", "Dark" };
        var cmbTheme = new ComboBox
        {
            ItemsSource = themes,
            SelectedItem = themes.Contains(config.Theme) ? config.Theme : "System",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var chkShowBars = new CheckBox { Content = L("ShowBars"), IsChecked = config.ShowBars };
        var chkShowStart = new CheckBox { Content = L("ShowStartButton"), IsChecked = config.ShowStartButton };
        var chkConfirmStart = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmStart, IsEnabled = config.ShowStartButton, Margin = new Thickness(24, 0, 0, 0) };
        var chkShowShutdown = new CheckBox { Content = L("ShowShutdownButton"), IsChecked = config.ShowShutdownButton };
        var chkConfirmShutdown = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmShutdown, IsEnabled = config.ShowShutdownButton, Margin = new Thickness(24, 0, 0, 0) };

        chkShowStart.IsCheckedChanged += (_, _) => chkConfirmStart.IsEnabled = chkShowStart.IsChecked == true;
        chkShowShutdown.IsCheckedChanged += (_, _) => chkConfirmShutdown.IsEnabled = chkShowShutdown.IsChecked == true;

        var tabAppearance = new TabItem
        {
            Header = L("TabAppearance"),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = L("Theme"), FontWeight = FontWeight.Bold },
                    cmbTheme,
                    chkShowBars,
                    new TextBlock { Text = L("PowerButtons"), FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 0) },
                    chkShowStart,
                    chkConfirmStart,
                    chkShowShutdown,
                    chkConfirmShutdown,
                }
            }
        };

        // ── Tab 2: Viewer ─────────────────────────────────────────────────
        var txtViewerPath = new TextBox
        {
            Text = config.ViewerPath,
            Watermark = L("SpiceViewerWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var btnBrowseViewer = new Button { Content = L("Browse") };
        var viewerRow = new Grid();
        viewerRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        viewerRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Avalonia.Controls.Grid.SetColumn(txtViewerPath, 0);
        Avalonia.Controls.Grid.SetColumn(btnBrowseViewer, 1);
        btnBrowseViewer.Margin = new Thickness(4, 0, 0, 0);
        viewerRow.Children.Add(txtViewerPath);
        viewerRow.Children.Add(btnBrowseViewer);

        var txtRdpPath = new TextBox
        {
            Text = config.RdpPath,
            Watermark = L("RdpClientWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var btnBrowseRdp = new Button { Content = L("Browse") };
        var rdpRow = new Grid();
        rdpRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        rdpRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Avalonia.Controls.Grid.SetColumn(txtRdpPath, 0);
        Avalonia.Controls.Grid.SetColumn(btnBrowseRdp, 1);
        btnBrowseRdp.Margin = new Thickness(4, 0, 0, 0);
        rdpRow.Children.Add(txtRdpPath);
        rdpRow.Children.Add(btnBrowseRdp);

        var tabConnectivity = new TabItem
        {
            Header = L("TabViewer"),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = L("SpiceViewerPath"), FontWeight = FontWeight.Bold },
                    viewerRow,
                    new TextBlock { Text = L("RdpClientPath"), FontWeight = FontWeight.Bold, Margin = new Thickness(0, 8, 0, 0) },
                    rdpRow,
                }
            }
        };

        // ── Tab 3: Clusters ───────────────────────────────────────────────
        var hostItems = new ObservableCollection<VdiHost>(config.Hosts);

        var lstHosts = new ListBox
        {
            ItemsSource = hostItems,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 160,
            ItemTemplate = new FuncDataTemplate<VdiHost>((h, _) =>
                new TextBlock { Text = h == null ? "" : $"{h.Name}  ({h.Hosts})" })
        };

        var btnAddHost = new Button { Content = Icons.WithText(Icons.Add, L("Add")), Margin = new Thickness(0, 0, 4, 0) };
        var btnEditHost = new Button { Content = Icons.WithText(Icons.Edit, L("Edit")), Margin = new Thickness(0, 0, 4, 0), IsEnabled = false };
        var btnDelHost = new Button { Content = Icons.WithText(Icons.Delete, L("Delete")), IsEnabled = false };

        lstHosts.SelectionChanged += (_, _) =>
        {
            btnEditHost.IsEnabled = lstHosts.SelectedIndex >= 0;
            btnDelHost.IsEnabled = lstHosts.SelectedIndex >= 0;
        };

        var tabClusters = new TabItem
        {
            Header = L("TabClusters"),
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

        // ── Buttons ───────────────────────────────────────────────────────
        var btnSave = new Button { Content = Icons.WithText(Icons.Save, L("Save")) };
        var btnCancel = new Button { Content = Icons.WithText(Icons.Close, L("Cancel")) };

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
            Items = { tabAppearance, tabConnectivity, tabClusters }
        };
        dock.Children.Add(tabControl);

        tabControl.SelectedIndex = initialTab;

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

        // ── Browse viewer ─────────────────────────────────────────────────
        btnBrowseViewer.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { Title = L("SelectSpiceViewer"), AllowMultiple = false });
            if (files.Count > 0) txtViewerPath.Text = files[0].Path.LocalPath;
        };

        btnBrowseRdp.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { Title = L("SelectRdpClient"), AllowMultiple = false });
            if (files.Count > 0) txtRdpPath.Text = files[0].Path.LocalPath;
        };

        // ── Add host ──────────────────────────────────────────────────────
        btnAddHost.Click += async (_, _) =>
        {
            var dlg = HostEditWindow.Create(null);
            dlg.Icon = MainWindowContext.AppIcon();
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

        // ── Edit host ─────────────────────────────────────────────────────
        btnEditHost.Click += async (_, _) =>
        {
            var idx = lstHosts.SelectedIndex;
            if (idx < 0 || idx >= config.Hosts.Count) return;
            var dlg = HostEditWindow.Create(config.Hosts[idx]);
            dlg.Icon = MainWindowContext.AppIcon();
            var result = await dlg.ShowDialog<VdiHost?>(window);
            if (result != null)
            {
                config.Hosts[idx] = result;
                hostItems[idx] = result;
                VdiConfigManager.Save(config);
                onHostsChanged?.Invoke();
                lstHosts.SelectedIndex = idx;
            }
        };

        // ── Delete host ───────────────────────────────────────────────────
        btnDelHost.Click += (_, _) =>
        {
            var idx = lstHosts.SelectedIndex;
            if (idx < 0 || idx >= config.Hosts.Count) return;
            config.Hosts.RemoveAt(idx);
            hostItems.RemoveAt(idx);
            VdiConfigManager.Save(config);
            onHostsChanged?.Invoke();
        };

        // ── Save ──────────────────────────────────────────────────────────
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
