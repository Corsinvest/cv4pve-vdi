/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Collections;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class UiHelper
{
    public static PathIcon Icon(string data, double size = 16, IBrush? foreground = null)
    {
        var icon = new PathIcon
        {
            Data = Geometry.Parse(data),
            Width = size,
            Height = size
        };
        if (foreground != null) { icon.Foreground = foreground; }
        return icon;
    }

    public static PathIcon Inner(string data)
        => new()
        {
            Data = Geometry.Parse(data),
            Width = 14,
            Height = 14,
            Margin = new Thickness(6, 0, 0, 0),
            Opacity = 0.5
        };

    public static Grid RowWithButton(Control main, Control button)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Add(main, 0);
        grid.Add(button, 1);
        return grid;
    }

    public static Grid WithIcon(Control control, string iconData)
    {
        var grid = new Grid();
        grid.Children.Add(control);
        grid.Children.Add(InnerOverlay(iconData));
        return grid;
    }

    public static PathIcon InnerOverlay(string data)
        => new()
        {
            Data = Geometry.Parse(data),
            Width = 14,
            Height = 14,
            Margin = new Thickness(8, 0, 0, 0),
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

    public static StackPanel WithText(string data, string text, IBrush? foreground = null)
    {
        var icon = new PathIcon
        {
            Data = Geometry.Parse(data),
            Width = 16,
            Height = 16
        };
        if (foreground != null) { icon.Foreground = foreground; }
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                icon,
                new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    public static (ComboBox Control, Grid WithIcon) ComboBoxWithIcon(IEnumerable items, string iconData, object? selectedItem = null)
    {
        var cmb = new ComboBox
        {
            ItemsSource = items,
            SelectedItem = selectedItem,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(26, 0, 0, 0)
        };
        return (cmb, WithIcon(cmb, iconData));
    }

    public static TextBox TextBox(string? text = null, string? watermark = null, string? iconData = null)
        => new()
        {
            Text = text ?? string.Empty,
            PlaceholderText = watermark ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = iconData != null ? Inner(iconData) : null
        };

    public static TextBlock Label(string key)
        => new()
        {
            Text = L(key),
            FontWeight = FontWeight.Bold
        };

    public static Button IconButton(string data,
                                    string? tooltipKey = null,
                                    Thickness? padding = null,
                                    Thickness? margin = null,
                                    double size = 16,
                                    Color? foregroundColor = null)
    {
        var btn = new Button
        {
            Content = Icon(data,
                           size,
                           foregroundColor.HasValue
                                ? new SolidColorBrush(foregroundColor.Value)
                                : null),
            Padding = padding ?? new Thickness(6, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        if (margin.HasValue) { btn.Margin = margin.Value; }
        if (tooltipKey != null) { ToolTip.SetTip(btn, L(tooltipKey)); }
        return btn;
    }
}
