/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

/// <summary>
/// Helpers to gate sensitive actions behind the kiosk admin password.
/// </summary>
internal static class KioskGuard
{
    /// <summary>
    /// Sticky admin unlock for the current process. Once set, all <see cref="CheckAsync"/> calls
    /// pass without prompting until the admin explicitly logs out (Switch user) or the app exits.
    /// </summary>
    public static bool IsAdminUnlocked { get; private set; }

    /// <summary>Clears the sticky admin unlock (call on Switch user / logout).</summary>
    public static void ResetAdmin() => IsAdminUnlocked = false;

    /// <summary>
    /// If kiosk mode is active and the admin is not already unlocked for this session,
    /// prompts the user for the admin password and returns true only on a correct answer.
    /// Outside kiosk mode, always returns true.
    /// </summary>
    public static async Task<bool> CheckAsync(Window owner, AppConfig config)
    {
        if (!config.Kiosk) { return true; }
        if (string.IsNullOrEmpty(config.KioskAdminPasswordHash)) { return true; }
        if (IsAdminUnlocked) { return true; }

        var entered = await PromptPasswordAsync(owner);
        if (entered is null) { return false; }

        if (PasswordHasher.Verify(entered, config.KioskAdminPasswordHash))
        {
            IsAdminUnlocked = true;
            return true;
        }

        await DialogHelper.MessageAsync(owner, L("KioskUnlockWrong"), NotificationSeverity.Error);
        return false;
    }

    private static async Task<string?> PromptPasswordAsync(Window owner)
    {
        var tcs = new TaskCompletionSource<string?>();

        var txt = UiHelper.TextBox(string.Empty, L("KioskAdminPassword"), AppIcons.Lock);
        txt.PasswordChar = '●';

        var btnOk = new Button { Content = L("Ok"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var btnCancel = new Button { Content = L("Cancel"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

        var dlg = new Window
        {
            Title = L("KioskUnlockPrompt"),
            Width = 340,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = L("KioskUnlockPrompt"), TextWrapping = TextWrapping.Wrap },
                    txt,
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 8,
                        Children            = { btnOk, btnCancel }
                    }
                }
            }
        };

        dlg.Opened += (_, _) => txt.Focus();

        btnOk.Click += (_, _) => { tcs.TrySetResult(txt.Text ?? string.Empty); dlg.Close(); };
        btnCancel.Click += (_, _) => { tcs.TrySetResult(null); dlg.Close(); };
        txt.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { tcs.TrySetResult(txt.Text ?? string.Empty); dlg.Close(); }
        };
        dlg.Closed += (_, _) => tcs.TrySetResult(null);

        await dlg.ShowDialog(owner);
        return await tcs.Task;
    }
}
