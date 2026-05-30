/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    // Strip shown between the top banner and the cards/list area, listing all
    // viewer/launcher processes spawned by cv4pve-vdi. Click a pill = bring its
    // window to the foreground; click the X = kill the process.
    // The strip is hidden when there are no open sessions.
    private Border? _sessionsBanner;
    private WrapPanel? _sessionsPills;

    internal Border BuildSessionsBanner()
    {
        _sessionsPills = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            ItemSpacing = 6,
            LineSpacing = 6
        };

        var label = new TextBlock
        {
            Text = L("OpenSessions"),
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        _sessionsBanner = new Border
        {
            Padding = new Thickness(14, 7),
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
            IsVisible = false,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { label, _sessionsPills }
            }
        };

        _sessions.Changed += RefreshSessionsBanner;
        RefreshSessionsBanner();

        return _sessionsBanner;
    }

    private void RefreshSessionsBanner()
    {
        if (_sessionsBanner is null || _sessionsPills is null) { return; }

        _sessionsPills.Children.Clear();
        foreach (var entry in _sessions.Sessions)
        {
            _sessionsPills.Children.Add(BuildSessionPill(entry));
        }
        _sessionsBanner.IsVisible = _sessions.Sessions.Count > 0;
    }

    private static Border BuildSessionPill(SessionTracker.Entry entry)
    {
        var serviceIcon = BuildServiceIcon(entry.IconData);

        var text = new TextBlock
        {
            Text = $"{entry.VmName} · {entry.ServiceLabel}",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var btnClose = new Button
        {
            Content = UiHelper.Icon(AppIcons.Close, 10),
            Padding = new Thickness(4, 2),
            Margin = new Thickness(4, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Opacity = 0.7,
            [ToolTip.TipProperty] = L("CloseSession")
        };
        btnClose.Click += (_, e) =>
        {
            e.Handled = true; // do not also fire the pill's Tapped (bring-to-front)
            SessionTracker.Close(entry);
        };

        var inner = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 6
        };
        if (!string.IsNullOrEmpty(entry.OsType))
        {
            // Same helper used by the VM type badge → consistent icon + colour.
            inner.Children.Add(BuildOsIcon(entry.OsType));
        }
        inner.Children.Add(serviceIcon);
        inner.Children.Add(text);
        inner.Children.Add(btnClose);

        var pill = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 2, 4, 2),
            Background = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128)),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            [ToolTip.TipProperty] = $"{L("BringToFront")} (PID {entry.Process.Id})",
            Child = inner
        };

        pill.Tapped += (_, _) => SessionTracker.Activate(entry);

        return pill;
    }
}
