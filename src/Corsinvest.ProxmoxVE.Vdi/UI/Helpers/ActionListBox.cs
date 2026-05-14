/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

/// <summary>
/// Defines a button shown in the toolbar above the list (Add, Reset, etc.)
/// </summary>
internal sealed class ToolbarButton
{
    public required string Icon { get; init; }
    public required string Tooltip { get; init; }
    public required Func<Task> OnClick { get; init; }
}

/// <summary>
/// Defines a button shown per-row on hover (Edit, Delete, Docs, etc.)
/// </summary>
internal sealed class RowButton<T>
{
    public required string Icon { get; init; }
    public required string Tooltip { get; init; }
    public required Func<T, Task> OnClick { get; init; }
    public IBrush? Foreground { get; init; }
    public Func<T, bool>? IsVisible { get; init; }
    public bool IsDoubleClick { get; init; }
}

/// <summary>
/// Reusable list control with hover-reveal row buttons (edit, delete, custom)
/// and a configurable toolbar.
/// </summary>
internal static class ActionListBox
{
    public static (StackPanel Panel, Action Refresh) Build<T>(IList<T> items,
                                                              Func<T, string> label,
                                                              IList<ToolbarButton> toolbarButtons,
                                                              IList<RowButton<T>> rowButtons,
                                                              int visibleRows = 6)
    {
        const double rowHeight = 32.0;
        var stackRows = new StackPanel { Spacing = 0 };

        var scroll = new ScrollViewer
        {
            MaxHeight = rowHeight * visibleRows,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stackRows
        };

        void Refresh()
        {
            stackRows.Children.Clear();
            foreach (var item in items)
            {
                var itemCopy = item;
                var row = BuildRow(itemCopy, label, rowButtons);
                stackRows.Children.Add(row);
            }
        }

        Refresh();

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        foreach (var tb in toolbarButtons)
        {
            var btn = new Button
            {
                Content = UiHelper.Icon(tb.Icon, 14),
                Padding = new Thickness(4),
                Background = Brushes.Transparent
            };
            ToolTip.SetTip(btn, tb.Tooltip);
            var tbCopy = tb;
            btn.Click += async (_, _) => await tbCopy.OnClick();
            toolbar.Children.Add(btn);
        }

        var panel = new StackPanel
        {
            Spacing = 6,
            Children = { toolbar, scroll }
        };

        return (panel, Refresh);
    }

    private static Border BuildRow<T>(T item, Func<T, string> label, IList<RowButton<T>> rowButtons)
    {
        var txtLabel = new TextBlock
        {
            Text = label(item),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        foreach (var rb in rowButtons)
        {
            var visible = rb.IsVisible?.Invoke(item) ?? true;
            if (!visible) { continue; }

            var btn = new Button
            {
                Content = UiHelper.Icon(rb.Icon, 14, rb.Foreground),
                Padding = new Thickness(4),
                Background = Brushes.Transparent
            };
            ToolTip.SetTip(btn, rb.Tooltip);
            var rbCopy = rb;
            var itemCopy = item;
            btn.Click += async (_, _) => await rbCopy.OnClick(itemCopy);
            btnPanel.Children.Add(btn);
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(txtLabel, 0);
        Grid.SetColumn(btnPanel, 1);
        grid.Children.Add(txtLabel);
        grid.Children.Add(btnPanel);

        var border = new Border
        {
            Padding = new Thickness(8, 0),
            Height = 32,
            CornerRadius = new CornerRadius(4),
            Child = grid
        };

        border.PointerEntered += (_, _) =>
        {
            border.Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
            btnPanel.IsVisible = true;
        };

        border.PointerExited += (_, _) =>
        {
            border.Background = Brushes.Transparent;
            btnPanel.IsVisible = false;
        };

        var dblClick = rowButtons.FirstOrDefault(rb => rb.IsDoubleClick && (rb.IsVisible?.Invoke(item) ?? true));
        if (dblClick is not null)
        {
            border.DoubleTapped += async (_, _) => await dblClick.OnClick(item);
        }

        return border;
    }
}
