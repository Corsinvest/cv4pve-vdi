/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class DialogHelper
{
    public static async Task MessageAsync(Window owner, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        var titleKey = severity switch
        {
            NotificationSeverity.Error => "Error",
            NotificationSeverity.Warning => "Warning",
            _ => "Information",
        };

        IBrush iconBrush = severity switch
        {
            NotificationSeverity.Error => new SolidColorBrush(Color.FromRgb(0xE8, 0x1C, 0x1C)),
            NotificationSeverity.Warning => new SolidColorBrush(Color.FromRgb(0xEA, 0xA1, 0x00)),
            _ => new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)),
        };

        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = L(titleKey),
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnOk = new Button { Content = L("Ok"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        btnOk.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(true);

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                UiHelper.Icon(AppIcons.Info, 24, iconBrush),
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }
            }
        };

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                header,
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing             = 12,
                    Children            = { btnOk }
                }
            }
        };

        await dlg.ShowDialog(owner);
        await tcs.Task;
    }

    public static async Task<bool> ConfirmAsync(Window owner, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = L("Confirm"),
            Width = 320,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnYes = new Button { Content = L("Yes"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var btnNo = new Button { Content = L("No"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

        btnYes.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        btnNo.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(false);

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing             = 12,
                    Children            = { btnYes, btnNo }
                }
            }
        };

        await dlg.ShowDialog(owner);
        return await tcs.Task;
    }
}
