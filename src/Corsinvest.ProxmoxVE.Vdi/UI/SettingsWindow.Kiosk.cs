/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Platform.Storage;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    private static (TabItem Tab, Func<string?> Save) BuildTabKiosk(AppConfig config, Window owner)
    {
        var chkEnable = new CheckBox
        {
            Content = UiHelper.WithText(AppIcons.Lock, L("KioskEnable")),
            IsChecked = config.Kiosk
        };

        var hasExistingPassword = !string.IsNullOrEmpty(config.KioskAdminPasswordHash);

        var txtPassword = UiHelper.TextBox(string.Empty,
                                            hasExistingPassword ? L("KioskPasswordSet") : L("KioskAdminPassword"),
                                            AppIcons.Key);
        txtPassword.PasswordChar = '●';

        var txtConfirm = UiHelper.TextBox(string.Empty,
                                           hasExistingPassword ? L("KioskPasswordSet") : L("KioskConfirmPassword"),
                                           AppIcons.Key);
        txtConfirm.PasswordChar = '●';

        var chkForceFullScreen = new CheckBox
        {
            Content = UiHelper.WithText(AppIcons.Fullscreen, L("KioskForceFullScreen")),
            IsChecked = config.KioskForceFullScreen,
            IsVisible = config.Kiosk
        };

        // Login background image picker
        var txtBackground = UiHelper.TextBox(config.KioskLoginBackground, L("KioskBackgroundWatermark"), AppIcons.Monitor);
        var btnBrowseBackground = UiHelper.IconButton(AppIcons.Folder, "KioskBackgroundBrowse", margin: new Thickness(4, 0, 0, 0));
        var backgroundRow = UiHelper.RowWithButton(txtBackground, btnBrowseBackground);

        btnBrowseBackground.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(owner);
            if (topLevel == null) { return; }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = L("KioskBackgroundBrowse"),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"] }
                ]
            });

            if (files.Count > 0) { txtBackground.Text = files[0].Path.LocalPath; }
        };

        var enableRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Children = { chkEnable, chkForceFullScreen }
        };

        var kioskOptionsPanel = new StackPanel
        {
            Spacing = 8,
            IsVisible = config.Kiosk,
            Children =
            {
                UiHelper.Label("KioskAdminPassword"), txtPassword,
                UiHelper.Label("KioskConfirmPassword"), txtConfirm,
                UiHelper.Label("KioskLoginBackground"), backgroundRow,
            }
        };

        chkEnable.IsCheckedChanged += (_, _) =>
        {
            var on = chkEnable.IsChecked is true;
            kioskOptionsPanel.IsVisible = on;
            chkForceFullScreen.IsVisible = on;
        };

        const string docsUrl = "https://github.com/Corsinvest/cv4pve-vdi/blob/master/docs/KIOSK.md";

        var description = new HyperlinkButton
        {
            Content = L("KioskDocumentationLink"),
            NavigateUri = new Uri(docsUrl),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 4, 0, 8)
        };

        var tab = new TabItem
        {
            Header = UiHelper.WithText(AppIcons.Monitor, L("TabKiosk")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    description,
                    enableRow,
                    kioskOptionsPanel,
                }
            }
        };

        string? Save()
        {
            var enable = chkEnable.IsChecked is true;
            var pwd = txtPassword.Text ?? string.Empty;
            var confirm = txtConfirm.Text ?? string.Empty;

            // Always allow background path / full-screen change regardless of enable/disable
            config.KioskLoginBackground = txtBackground.Text?.Trim() ?? string.Empty;
            config.KioskForceFullScreen = chkForceFullScreen.IsChecked is true;

            if (!enable)
            {
                config.Kiosk = false;
                // keep the existing hash so re-enabling later doesn't force a password reset
                return null;
            }

            // Enabling kiosk
            if (string.IsNullOrEmpty(pwd) && string.IsNullOrEmpty(confirm))
            {
                if (!hasExistingPassword) { return L("KioskPasswordRequired"); }
                // keep existing
                config.Kiosk = true;
                return null;
            }

            if (pwd != confirm) { return L("KioskPasswordMismatch"); }
            if (string.IsNullOrEmpty(pwd)) { return L("KioskPasswordRequired"); }

            config.Kiosk = true;
            config.KioskAdminPasswordHash = PasswordHasher.Hash(pwd);
            return null;
        }

        return (tab, Save);
    }
}
