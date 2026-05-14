/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    // toast container — absolute overlay in bottom-right corner
    private readonly StackPanel _toastStack = new()
    {
        Spacing = 8,
        VerticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 0, 20, 20),
        ZIndex = 200
    };

    // persistent banner — sits below topbar, no close button
    private Border? _persistentBanner;
    private TextBlock? _persistentBannerText;

    /// <summary>Show a temporary toast that auto-dismisses after <paramref name="seconds"/> seconds.</summary>
    internal void ShowToast(string message, NotificationSeverity severity = NotificationSeverity.Info, int seconds = 5)
    {
        var accentColor = severity switch
        {
            NotificationSeverity.Warning => AppColors.NotifyWarning,
            NotificationSeverity.Error => AppColors.NotifyError,
            _ => AppColors.NotifyInfo
        };

        var isDark = AppColors.IsDark;
        var dimFg = new SolidColorBrush(Color.FromArgb(160, 150, 150, 150));

        var copyBtn = new Button
        {
            Content = UiHelper.Icon(AppIcons.Clipboard2, 11),
            Padding = new Thickness(5, 2),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = dimFg,
            VerticalAlignment = VerticalAlignment.Top,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(copyBtn, L("Copy"));

        var closeBtn = new Button
        {
            Content = "✕",
            Padding = new Thickness(5, 2),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = dimFg,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Top,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // accent bar on the left
        var accentBar = new Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2, 0, 0, 2),
            Background = new SolidColorBrush(accentColor),
            Margin = new Thickness(-12, -8, 8, -8)
        };

        var toast = new Border
        {
            Background = new SolidColorBrush(isDark
                                                ? AppColors.ToastBgDark
                                                : AppColors.ToastBgLight),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10),
            MinWidth = 280,
            MaxWidth = 380,
            BoxShadow = BoxShadows.Parse("0 4 16 0 #40000000"),
            BorderBrush = new SolidColorBrush(isDark
                            ? Color.FromArgb(60, 255, 255, 255)
                            : Color.FromArgb(40, 0, 0, 0)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    accentBar,
                    new PathIcon
                    {
                        Data = Geometry.Parse(AppIcons.Info),
                        Width = 16,
                        Height = 16,
                        Foreground = new SolidColorBrush(accentColor),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = message,
                        Foreground = isDark
                                        ? Brushes.White
                                        : new SolidColorBrush(Color.Parse("#1e293b")),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        MaxWidth = 260,
                        LineHeight = 18
                    },
                    copyBtn,
                    closeBtn
                }
            }
        };

        _toastStack.Children.Add(toast);

        copyBtn.Click += async (_, _) =>
        {
            var cb = TopLevel.GetTopLevel(_window)?.Clipboard;
            if (cb != null) { await cb.SetTextAsync(message); }
        };
        closeBtn.Click += (_, _) => _toastStack.Children.Remove(toast);

        // auto dismiss
        _ = Task.Delay(seconds * 1000)
                .ContinueWith(_ => Dispatcher.UIThread.Post(() => _toastStack.Children.Remove(toast)));
    }

    /// <summary>Show a persistent banner (no auto-dismiss). Call <see cref="HideBanner"/> to remove it.</summary>
    internal void ShowBanner(string message, NotificationSeverity severity = NotificationSeverity.Warning)
    {
        if (_persistentBanner == null) { return; }

        var isDark = AppColors.IsDark;
        var (bg, border, fg) = severity switch
        {
            NotificationSeverity.Error => isDark
                    ? (AppColors.BannerErrorBgDark, AppColors.NotifyError, Colors.White)
                    : (AppColors.BannerErrorBgLight, AppColors.NotifyError, AppColors.BannerErrorFgLight),

            NotificationSeverity.Info => isDark
                    ? (AppColors.BannerInfoBgDark, AppColors.NotifyInfo, Colors.White)
                    : (AppColors.BannerInfoBgLight, AppColors.NotifyInfo, AppColors.BannerInfoFgLight),
            _ => isDark
                ? (AppColors.BannerWarningBgDark, AppColors.NotifyWarning, Colors.White)
                : (AppColors.BannerWarningBgLight, AppColors.NotifyWarning, AppColors.BannerWarningFgLight)
        };

        _persistentBanner.Background = new SolidColorBrush(bg);
        _persistentBanner.BorderBrush = new SolidColorBrush(border);
        if (_persistentBannerText != null)
        {
            _persistentBannerText.Text = message;
            _persistentBannerText.Foreground = new SolidColorBrush(fg);
        }
        _persistentBanner.IsVisible = true;
    }

    /// <summary>Hide the persistent banner.</summary>
    internal void HideBanner() => _persistentBanner?.IsVisible = false;

    internal Border BuildPersistentBanner(Button actionBtn)
    {
        _persistentBannerText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 12
        };

        var bannerIcon = UiHelper.Icon(AppIcons.Info, 14, new SolidColorBrush(AppColors.NotifyWarning));

        _persistentBanner = new Border
        {
            Padding = new Thickness(14, 7),
            BorderThickness = new Thickness(0, 0, 0, 1),
            IsVisible = false,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4,
                Children =
                {
                    bannerIcon,
                    _persistentBannerText,
                    actionBtn
                }
            }
        };

        return _persistentBanner;
    }
}
