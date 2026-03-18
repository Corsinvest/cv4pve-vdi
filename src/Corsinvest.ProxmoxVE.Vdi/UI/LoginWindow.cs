/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

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
            InnerLeftContent = AppIcons.Inner(AppIcons.Account)
        };

        var txtPassword = new TextBox
        {
            PasswordChar = '●',
            Watermark = L("Password"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Lock)
        };

        var txtOtp = new TextBox
        {
            Watermark = L("OtpWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Key)
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
            Content = AppIcons.WithText(AppIcons.Login, L("Login")),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var btnSettings = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Settings),
            Padding = new Thickness(6, 4)
        };

        Avalonia.Controls.ToolTip.SetTip(btnSettings, L("ManageHosts"));

        // Host row: ComboBox (with icon overlay) + settings button
        var cmbHostWithIcon = new Grid();
        cmbHost.Padding = new Thickness(26, 0, 0, 0);
        var hostIcon = AppIcons.InnerOverlay(AppIcons.Server);
        cmbHostWithIcon.Children.Add(cmbHost);
        cmbHostWithIcon.Children.Add(hostIcon);

        var hostRow = new Grid();
        hostRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        hostRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        cmbHostWithIcon.Margin = new Thickness(0, 0, 4, 0);
        btnSettings.Margin = new Thickness(2, 0, 0, 0);
        hostRow.Add(cmbHostWithIcon, 0);
        hostRow.Add(btnSettings, 1);

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
                    new ProgressBar
                    {
                        IsIndeterminate = true,
                        Width = 200
                    },
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
                new TextBlock
                {
                    Text = L("AppTitle"),
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                },
                new TextBlock { Text = L("Host") },
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

        var window = new Window
        {
            Title = $"{L("LoginWindowTitle")} v{ApplicationHelper.Version}",
            Width = 480,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Panel
            {
                Children =
                {
                    form,
                    busyOverlay
                }
            }
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
            var dlg = SettingsWindow.Create(config, RefreshHostList, clustersOnly: true);
            dlg.Icon = MainWindow.AppIcon();
            await dlg.ShowDialog(window!);
        };

        async Task DoLogin()
        {
            lblError.Text = " ";
            btnLogin.IsEnabled = false;
            busyOverlay.IsVisible = true;

            var idx = cmbHost.SelectedIndex;
            var host = idx >= 0 && idx < config.Hosts.Count
                ? config.Hosts[idx]
                : null;

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

            var (client, error) = await ConnectAsync(host, user, pwd, otp);
            busyOverlay.IsVisible = false;
            btnLogin.IsEnabled = true;

            if (client == null)
            {
                lblError.Text = error;
                return;
            }

            config.LastUser = user;
            VdiConfigManager.Save(config);

            var mainWin = new MainWindow(client, host, config).Build();
            mainWin.Show();
            window!.Close();
        }

        btnLogin.Click += async (_, _) => await DoLogin();
        txtPassword.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await DoLogin();
            }
        };
        window.Opened += (_, _) => txtPassword.Focus();

        return window;
    }

    private static async Task<(PveClient? Client, string Error)> ConnectAsync(VdiHost host,
                                                                              string username,
                                                                              string password,
                                                                              string otp = "")
    {
        try
        {
            var (client, _) = await ClientHelper.GetClientFromHAAsync(host.Hosts, host.Timeout * 1000);
            if (client == null)
            {
                return (null, "No reachable hosts found");
            }

            client.ValidateCertificate = !host.SkipSslValidation;
            var otpValue = string.IsNullOrWhiteSpace(otp)
                ? null
                : otp;
            if (!await client.LoginAsync(username, password, otpValue))
            {
                return (null, client.LastResult?.ReasonPhrase ?? "Authentication failed");
            }

            return (client, string.Empty);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }
}
