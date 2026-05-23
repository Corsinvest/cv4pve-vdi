/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class ClusterEditWindow
{
    /// <summary>
    /// Pass null for Add, or an existing ClusterConfig for Edit.
    /// Returns the saved ClusterConfig or null if cancelled.
    /// </summary>
    public static Window Create(ClusterConfig? existing)
    {
        ReapplyLanguage();
        var isEdit = existing != null;

        var txtName = UiHelper.TextBox(existing?.Name, "e.g. prod", AppIcons.Server);
        var txtUrl = UiHelper.TextBox(existing?.Hosts, "pve1.example.com:8006", AppIcons.Network);

        var chkSkipSsl = new CheckBox
        {
            Content = UiHelper.WithText(AppIcons.Lock, L("HostSkipSsl")),
            IsChecked = existing?.SkipSslValidation ?? false
        };

        var numTimeout = new NumericUpDown
        {
            Value = existing?.Timeout ?? 10,
            Minimum = 5,
            Maximum = 120,
            Increment = 5,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numTimeoutWithIcon = UiHelper.WithIcon(numTimeout, AppIcons.Clock);

        var txtProxy = UiHelper.TextBox(existing?.Spice.Proxy, "host:port or https://host:port (empty = use PVE host)", AppIcons.Ethernet);
        var txtViewerOptions = UiHelper.TextBox(existing?.Spice.ViewerOptions, L("HostViewerOptionsWatermark"), AppIcons.Console);

        var lblError = new TextBlock
        {
            Foreground = Brushes.Red,
            IsVisible = false
        };

        var btnSave = UiHelper.IconButton(isEdit
                                            ? AppIcons.Save
                                            : AppIcons.Add,
                                        isEdit
                                            ? "Save"
                                            : "Add");

        var window = new Window
        {
            Title = L(isEdit ? "EditHost" : "AddHost"),
            Width = 420,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    UiHelper.Label("HostName"), txtName,
                    UiHelper.Label("HostHosts"), txtUrl,
                    chkSkipSsl,
                    UiHelper.Label("HostTimeout"), numTimeoutWithIcon,
                    UiHelper.Label("HostSpiceProxy"), txtProxy,
                    UiHelper.Label("HostViewerOptions"), txtViewerOptions,
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
            var name = txtName.Text?.Trim() ?? string.Empty;
            var url = txtUrl.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                lblError.Text = L("HostNameRequired");
                lblError.IsVisible = true;
                return;
            }

            var result = new ClusterConfig
            {
                Name = name,
                Hosts = url,
                SkipSslValidation = chkSkipSsl.IsChecked is true,
                Timeout = (int)(numTimeout.Value ?? 10),
                Spice = new SpiceConfig
                {
                    Proxy = txtProxy.Text?.Trim() ?? string.Empty,
                    ViewerOptions = txtViewerOptions.Text?.Trim() ?? string.Empty
                }
            };

            window.Close(result);
        };

        return window;
    }
}
