/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Platform.Storage;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class LauncherEditWindow
{
    /// <summary>
    /// Pass null for <paramref name="existing"/> to create a new launcher.
    /// Returns the saved LauncherDefinition or null if cancelled.
    /// </summary>
    public static async Task<LauncherDefinition?> ShowAsync(Window owner, LauncherDefinition? existing = null)
    {
        var isNew = existing is null;

        var txtServiceId = UiHelper.TextBox(existing?.ServiceId, "e.g. my-rdp-client", AppIcons.Tag);
        txtServiceId.IsReadOnly = !isNew;

        var txtDisplayName = UiHelper.TextBox(existing?.DisplayName, "e.g. My RDP Client", AppIcons.Edit);

        var (cmbPlatform, cmbPlatformWithIcon) = UiHelper.ComboBoxWithIcon(Enum.GetValues<LauncherPlatform>(),
                                                                           AppIcons.Server,
                                                                           existing?.Platform ?? LauncherPlatform.Windows);

        // Default port
        var numPort = new NumericUpDown
        {
            Value = existing?.DefaultPort ?? 22,
            Minimum = 1,
            Maximum = 65535,
            Increment = 1,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numPortWithIcon = UiHelper.WithIcon(numPort, AppIcons.Ethernet);

        // Executable
        var txtExecutable = UiHelper.TextBox(existing?.Executable, L("ExeWatermark"), AppIcons.Console);

        var btnBrowseExecutable = UiHelper.IconButton(AppIcons.Folder, "BrowseExecutable", margin: new Thickness(4, 0, 0, 0));

        var executableRow = UiHelper.RowWithButton(txtExecutable, btnBrowseExecutable);

        // Arguments
        // {ip}:{port} {?{username}} {extraArgs}
        // {?TEXT} = includi TEXT (con token risolti) solo se tutti i token interni sono non vuoti
        var txtArguments = UiHelper.TextBox(existing?.Arguments, "{ip}:{port} {?{username}} {extraArgs}", AppIcons.Edit);

        // Extra args
        var txtExtraArgs = UiHelper.TextBox(existing?.ExtraArgs, L("ExtraArgsWatermark"), AppIcons.Tune);

        // Flags
        var chkCredentials = new CheckBox
        {
            Content = L("SupportsCredentials"),
            IsChecked = existing?.SupportsCredentials ?? false
        };

        var chkWinCredential = new CheckBox
        {
            Content = L("UseWindowsCredential"),
            IsChecked = existing?.WindowsCredential.Enable ?? false,
            IsVisible = (existing?.Platform ?? LauncherPlatform.Windows) == LauncherPlatform.Windows
        };

        // Windows Credential details (Type, Target)
        var (cmbWinCredType, cmbWinCredTypeWithIcon) = UiHelper.ComboBoxWithIcon(Enum.GetValues<WindowsCredentialType>(),
                                                                                  AppIcons.Key,
                                                                                  existing?.WindowsCredential.Type ?? WindowsCredentialType.Generic);

        var txtWinCredTarget = UiHelper.TextBox(existing?.WindowsCredential.Target ?? "{ip}", "{ip} or TERMSRV/{ip}", AppIcons.Network);

        var winCredPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(20, 0, 0, 0),
            IsVisible = chkWinCredential.IsChecked is true && chkWinCredential.IsVisible,
            Children =
            {
                UiHelper.Label("WinCredentialType"), cmbWinCredTypeWithIcon,
                UiHelper.Label("WinCredentialTarget"), txtWinCredTarget,
            }
        };

        void UpdateWinCredPanel()
            => winCredPanel.IsVisible = chkWinCredential.IsVisible && chkWinCredential.IsChecked is true;

        chkWinCredential.IsCheckedChanged += (_, _) => UpdateWinCredPanel();
        cmbPlatform.SelectionChanged += (_, _) =>
        {
            chkWinCredential.IsVisible = cmbPlatform.SelectedItem is LauncherPlatform.Windows;
            UpdateWinCredPanel();
        };

        // Error / Save
        var lblError = new TextBlock { Foreground = Brushes.Red, IsVisible = false };

        var btnSave = UiHelper.IconButton(isNew ? AppIcons.Add : AppIcons.Save, isNew ? "Add" : "Save");

        var window = new Window
        {
            Title = isNew
                    ? L("NewLauncher")
                    : $"{L("EditLauncher")}: {existing!.DisplayName}",

            Width = 440,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    UiHelper.Label("LauncherServiceId"), txtServiceId,
                    UiHelper.Label("LauncherDisplayName"), txtDisplayName,
                    UiHelper.Label("LauncherPlatform"), cmbPlatformWithIcon,
                    UiHelper.Label("LauncherDefaultPort"), numPortWithIcon,
                    UiHelper.Label("LauncherExecutable"), executableRow,
                    UiHelper.Label("LauncherArguments"), txtArguments,
                    UiHelper.Label("ExtraArgs"), txtExtraArgs,
                    chkCredentials,
                    chkWinCredential,
                    winCredPanel,
                    lblError,
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 8,
                        Children            = { btnSave }
                    }
                }
            }
        };

        btnBrowseExecutable.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) { return; }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = L("BrowseExecutable"),
                AllowMultiple = false
            });

            if (files.Count > 0) { txtExecutable.Text = files[0].Path.LocalPath; }
        };

        btnSave.Click += (_, _) =>
        {
            var serviceId = txtServiceId.Text?.Trim() ?? string.Empty;
            var displayName = txtDisplayName.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(displayName))
            {
                lblError.Text = L("LauncherIdAndNameRequired");
                lblError.IsVisible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtExecutable.Text))
            {
                lblError.Text = L("LauncherExecutableRequired");
                lblError.IsVisible = true;
                return;
            }

            window.Close(new LauncherDefinition
            {
                ServiceId = serviceId,
                DisplayName = displayName,
                Platform = (LauncherPlatform)(cmbPlatform.SelectedItem ?? LauncherPlatform.Windows),
                DefaultPort = (int)(numPort.Value ?? 22),
                Executable = txtExecutable.Text!.Trim(),
                Arguments = txtArguments.Text?.Trim() ?? string.Empty,
                ExtraArgs = txtExtraArgs.Text?.Trim() ?? string.Empty,
                SupportsCredentials = chkCredentials.IsChecked is true,
                WindowsCredential = new WindowsCredentialDefinition
                {
                    Enable = chkWinCredential.IsChecked is true,
                    Type = cmbWinCredType.SelectedItem is WindowsCredentialType t ? t : WindowsCredentialType.Generic,
                    Target = string.IsNullOrWhiteSpace(txtWinCredTarget.Text) ? "{ip}" : txtWinCredTarget.Text!.Trim(),
                },
                DocumentationUrl = existing?.DocumentationUrl ?? string.Empty,
            });
        };

        return await window.ShowDialog<LauncherDefinition?>(owner);
    }
}
