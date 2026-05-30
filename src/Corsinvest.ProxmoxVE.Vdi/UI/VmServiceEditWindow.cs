/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Controls.Templates;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class VmServiceEditWindow
{
    public static async Task<VmServiceConfig?> ShowAsync(
        Window owner,
        VmServiceConfig? existing,
        IReadOnlyList<LauncherDefinition> launchers)
    {
        ReapplyLanguage();
        var isEdit = existing is not null;

        var cmbLauncher = new ComboBox
        {
            ItemsSource = launchers,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<LauncherDefinition>((def, _) => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new PathIcon
                    {
                        Data = Geometry.Parse(AppIcons.ForLauncher(def?.Icon ?? string.Empty)),
                        Width = 14,
                        Height = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = def?.DisplayName ?? string.Empty,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            })
        };

        if (isEdit)
        {
            var idx = launchers.ToList().FindIndex(l => l.ServiceId == existing!.ServiceId);
            cmbLauncher.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else if (launchers.Count > 0)
        {
            cmbLauncher.SelectedIndex = 0;
        }

        // Port
        var numPort = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Increment = 1,
            FormatString = "0",
            Value = existing?.Port > 0
                            ? existing.Port
                            : (launchers.Count > 0 ? launchers[0].DefaultPort : 22),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numPortWithIcon = UiHelper.WithIcon(numPort, AppIcons.Ethernet);

        cmbLauncher.SelectionChanged += (_, _) =>
        {
            if (cmbLauncher.SelectedItem is LauncherDefinition def && !isEdit)
            {
                numPort.Value = def.DefaultPort;
            }
        };

        // Credential source
        var (cmbCredSource, cmbCredSourceWithIcon) = UiHelper.ComboBoxWithIcon(Enum.GetValues<CredentialSource>(),
                                                                               AppIcons.Key,
                                                                               existing?.CredentialSource ?? CredentialSource.None);

        var txtIpOverride = UiHelper.TextBox(existing?.IpOverride, L("IpOverrideWatermark"), AppIcons.Network);
        var txtExtraArgs = UiHelper.TextBox(existing?.ExtraArgs, L("ExtraArgsWatermark"), AppIcons.Tune);
        var txtUsername = UiHelper.TextBox(existing?.Credentials?.Username, L("Username"), AppIcons.Account);

        var txtPassword = UiHelper.TextBox(existing?.Credentials?.Password, L("Password"), AppIcons.Lock);
        txtPassword.PasswordChar = '●';

        var credPanel = new StackPanel
        {
            Spacing = 8,
            IsVisible = existing?.CredentialSource == CredentialSource.Manual,
            Children = { txtUsername, txtPassword }
        };

        cmbCredSource.SelectionChanged += (_, _) => credPanel.IsVisible = cmbCredSource.SelectedItem is CredentialSource.Manual;

        // Error / Save
        var lblError = new TextBlock { Foreground = Brushes.Red, IsVisible = false };

        var btnSave = UiHelper.IconButton(isEdit ? AppIcons.Save : AppIcons.Add, isEdit ? "Save" : "Add");

        var window = new Window
        {
            Title = L(isEdit ? "EditService" : "AddService"),
            Width = 380,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    UiHelper.Label("Launcher"), cmbLauncher,
                    UiHelper.Label("Port"), numPortWithIcon,
                    UiHelper.Label("IpOverride"), txtIpOverride,
                    UiHelper.Label("ExtraArgs"), txtExtraArgs,
                    UiHelper.Label("CredentialSource"), cmbCredSourceWithIcon,
                    credPanel,
                    lblError,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { btnSave }
                    }
                }
            }
        };

        btnSave.Click += (_, _) =>
        {
            if (cmbLauncher.SelectedItem is not LauncherDefinition launcher)
            {
                lblError.Text = L("SelectLauncher");
                lblError.IsVisible = true;
                return;
            }

            var credSource = cmbCredSource.SelectedItem is CredentialSource cs
                                ? cs
                                : CredentialSource.None;

            Credentials? credentials = null;
            if (credSource == CredentialSource.Manual)
            {
                credentials = new Credentials
                {
                    Username = txtUsername.Text?.Trim() ?? string.Empty,
                    Password = txtPassword.Text?.Trim() ?? string.Empty
                };
            }

            window.Close(new VmServiceConfig
            {
                ServiceId = launcher.ServiceId,
                Port = (int)(numPort.Value ?? launcher.DefaultPort),
                IpOverride = txtIpOverride.Text?.Trim() ?? string.Empty,
                ExtraArgs = txtExtraArgs.Text?.Trim() ?? string.Empty,
                CredentialSource = credSource,
                Credentials = credentials
            });
        };

        return await window.ShowDialog<VmServiceConfig?>(owner);
    }
}
