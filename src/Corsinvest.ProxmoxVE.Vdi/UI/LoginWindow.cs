/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class LoginWindow
{
    public static Window Create(AppConfig config)
    {
        var (cmbCluster, cmbClusterWithIcon) = UiHelper.ComboBoxWithIcon(config.Clusters.ConvertAll(h => h.Name), AppIcons.Server);
        cmbCluster.SelectedIndex = 0;
        cmbClusterWithIcon.Margin = new Thickness(0, 0, 4, 0);

        var txtUser = UiHelper.TextBox(config.LastUser, "user@pam", AppIcons.Account);

        var txtPassword = UiHelper.TextBox(watermark: L("Password"), iconData: AppIcons.Lock);
        txtPassword.PasswordChar = '●';

        var txtOtp = UiHelper.TextBox(watermark: L("OtpWatermark"), iconData: AppIcons.Key);

        var lblError = new TextBlock
        {
            Foreground = Brushes.Red,
            IsVisible = true,
            Text = " ",
            MinHeight = 20
        };

        var btnLogin = new Button
        {
            Content = UiHelper.WithText(AppIcons.Login, L("Login")),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        var btnSettings = UiHelper.IconButton(AppIcons.Settings, "ManageClusters", margin: new Thickness(2, 0, 0, 0));
        var hostRow = UiHelper.RowWithButton(cmbClusterWithIcon, btnSettings);

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
                UiHelper.Label("ClusterConfig"), hostRow,
                UiHelper.Label("Username"), txtUser,
                UiHelper.Label("Password"), txtPassword,
                UiHelper.Label("OtpLabel"), txtOtp,
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

        var kioskLoginUnlocked = false;
        if (config.Kiosk)
        {
            if (config.KioskForceFullScreen)
            {
                form.Width = 480;
                form.HorizontalAlignment = HorizontalAlignment.Center;
                form.VerticalAlignment = VerticalAlignment.Center;

                window.SizeToContent = SizeToContent.Manual;
                window.WindowState = WindowState.FullScreen;
            }

            if (!string.IsNullOrEmpty(config.KioskLoginBackground) && File.Exists(config.KioskLoginBackground))
            {
                try
                {
                    var bg = new Avalonia.Media.Imaging.Bitmap(config.KioskLoginBackground);
                    window.Background = new ImageBrush(bg) { Stretch = Stretch.UniformToFill };
                }
                catch { /* invalid image — fall back to default background */ }
            }

            window.Closing += async (_, e) =>
            {
                if (kioskLoginUnlocked) { return; }
                e.Cancel = true;
                if (await KioskGuard.CheckAsync(window, config))
                {
                    kioskLoginUnlocked = true;
                    window.Close();
                }
            };
        }

        void RefreshHostList()
        {
            cmbCluster.ItemsSource = config.Clusters.ConvertAll(h => h.Name);
            cmbCluster.SelectedIndex = config.Clusters.Count > 0
                                        ? Math.Clamp(cmbCluster.SelectedIndex, 0, config.Clusters.Count - 1)
                                        : -1;
        }

        RefreshHostList();

        btnSettings.Click += async (_, _) =>
        {
            if (!await KioskGuard.CheckAsync(window!, config)) { return; }
            var dlg = SettingsWindow.Create(config, RefreshHostList, clustersOnly: true);
            dlg.Icon = MainWindow.AppIcon();
            await dlg.ShowDialog(window!);

            if (dlg.Tag as string == "reopen")
            {
                var dlg2 = SettingsWindow.Create(config, RefreshHostList, clustersOnly: true);
                dlg2.Icon = MainWindow.AppIcon();
                await dlg2.ShowDialog(window!);
            }
        };

        async Task DoLogin()
        {
            lblError.Text = " ";
            btnLogin.IsEnabled = false;
            busyOverlay.IsVisible = true;

            var idx = cmbCluster.SelectedIndex;
            var host = idx >= 0 && idx < config.Clusters.Count
                        ? config.Clusters[idx]
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
            AppConfigManager.Save(config);

            var mainWin = new MainWindow(client, host, config, user, pwd).Build();
            mainWin.Show();
            kioskLoginUnlocked = true;
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

    private static async Task<(PveClient? Client, string Error)> ConnectAsync(ClusterConfig host,
                                                                              string username,
                                                                              string password,
                                                                              string otp = "")
    {
        try
        {
            var (client, _) = await ClientHelper.GetClientFromHAAsync(host.Hosts, host.Timeout * 1000);
            if (client == null) { return (null, "No reachable hosts found"); }

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
