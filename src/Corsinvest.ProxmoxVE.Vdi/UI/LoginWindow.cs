/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Services;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class LoginWindow
{
    public static Window Create(VdiConfig config)
    {
        var cmbHost = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = config.Hosts.Select(h => h.Name).ToList(),
            SelectedIndex = 0
        };

        var txtUser = new TextBox
        {
            Text = config.LastUser,
            Watermark = "user@pam",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = new PathIcon
            {
                Data = Geometry.Parse(Icons.Account),
                Width = 14,
                Height = 14,
                Margin = new Thickness(6, 0, 0, 0),
                Opacity = 0.5
            }
        };

        var txtPassword = new TextBox
        {
            PasswordChar = '●',
            Watermark = L("Password"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = new PathIcon
            {
                Data = Geometry.Parse(Icons.Lock),
                Width = 14,
                Height = 14,
                Margin = new Thickness(6, 0, 0, 0),
                Opacity = 0.5
            }
        };

        var txtOtp = new TextBox
        {
            Watermark = L("OtpWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = new PathIcon
            {
                Data = Geometry.Parse(Icons.Key),
                Width = 14,
                Height = 14,
                Margin = new Thickness(6, 0, 0, 0),
                Opacity = 0.5
            }
        };

        var lblError = new TextBlock
        {
            Foreground = Brushes.Red,
            IsVisible = true,
            Text = " ",
            MinHeight = 20
        };

        var btnLogin = new Button
        {
            Content = Icons.WithText(Icons.Login, L("Login")),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var btnSettings = new Button
        {
            Content = Icons.Toolbar(Icons.Settings),
            Padding = new Thickness(6, 4)
        };

        Avalonia.Controls.ToolTip.SetTip(btnSettings, L("ManageHosts"));

        // Host row: ComboBox + settings button
        var hostRow = new Grid();
        hostRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        hostRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Avalonia.Controls.Grid.SetColumn(cmbHost, 0);
        Avalonia.Controls.Grid.SetColumn(btnSettings, 1);
        cmbHost.Margin = new Thickness(0, 0, 4, 0);
        btnSettings.Margin = new Thickness(2, 0, 0, 0);
        hostRow.Children.Add(cmbHost);
        hostRow.Children.Add(btnSettings);

        Window? window = null;

        var busyOverlay = new Border
        {
            IsVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new ProgressBar { IsIndeterminate = true, Width = 200 },
                    new TextBlock
                    {
                        Text = L("Connecting"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.White,
                        FontSize = 14
                    }
                }
            }
        };

        var form = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = L("AppTitle"), FontSize = 18, FontWeight = FontWeight.Bold },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new PathIcon { Data = Geometry.Parse(Icons.Server), Width = 14, Height = 14, Opacity = 0.7 },
                        new TextBlock { Text = L("Host"), VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                hostRow,
                new TextBlock { Text = L("Username") },
                txtUser,
                new TextBlock { Text = L("Password") },
                txtPassword,
                new TextBlock { Text = L("OtpLabel") },
                txtOtp,
                lblError,
                btnLogin
            }
        };

        window = new Window
        {
            Title = L("LoginWindowTitle"),
            Width = 480,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Panel { Children = { form, busyOverlay } }
        };

        void RefreshHostList()
        {
            var prevIdx = cmbHost.SelectedIndex;
            cmbHost.ItemsSource = config.Hosts.Select(h => h.Name).ToList();
            cmbHost.SelectedIndex = config.Hosts.Count > 0
                                        ? Math.Clamp(prevIdx, 0, config.Hosts.Count - 1)
                                        : -1;
        }

        RefreshHostList();

        btnSettings.Click += async (_, _) =>
        {
            var dlg = SettingsWindow.Create(config, RefreshHostList, initialTab: 2);
            dlg.Icon = MainWindowContext.AppIcon();
            await dlg.ShowDialog(window!);
        };

        async Task DoLogin()
        {
            lblError.Text = " ";
            btnLogin.IsEnabled = false;
            busyOverlay.IsVisible = true;

            var idx = cmbHost.SelectedIndex;
            var host = idx >= 0 && idx < config.Hosts.Count ? config.Hosts[idx] : null;

            if (host == null)
            {
                busyOverlay.IsVisible = false;
                lblError.Text = L("PleaseSelectHost");
                btnLogin.IsEnabled = true;
                return;
            }

            var user = txtUser.Text ?? string.Empty;
            var pwd = txtPassword.Text ?? string.Empty;
            var otp = txtOtp.Text ?? string.Empty;

            var (client, error) = await RemoteViewerService.ConnectAsync(host, user, pwd, otp);
            busyOverlay.IsVisible = false;
            btnLogin.IsEnabled = true;

            if (client == null)
            {
                lblError.Text = error;
                return;
            }

            config.LastUser = user;
            VdiConfigManager.Save(config);

            var mainWin = MainWindow.Create(client, host, config);
            mainWin.Show();
            window!.Close();
        }

        btnLogin.Click += async (_, _) => await DoLogin();
        txtPassword.KeyDown += async (_, e) => { if (e.Key == Key.Enter) { await DoLogin(); } };

        window.Opened += (_, _) => txtPassword.Focus();

        return window;
    }
}
