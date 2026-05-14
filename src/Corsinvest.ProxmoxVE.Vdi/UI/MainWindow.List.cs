/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static Control BuildInlineBar(double pct, string text)
        => new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children =
        {
            new ProgressBar
            {
                Value = pct, Maximum = 100, Minimum = 0, Height = 3,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Foreground = new SolidColorBrush(AppColors.BarColor(pct))
            },
            new TextBlock
            {
                Text = text, FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        }
        };

    private Control BuildListRow(ResourceRow r)
    {
        var statusDotPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusDotPanel.Children.Add(BuildStatusDot(r));
        BuildAgentBadge(r, statusDotPanel);

        var idLbl = new TextBlock
        {
            Text = r.IdDisplay,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 50
        }.Secondary();

        var nameLbl = new TextBlock
        {
            Text = r.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Width = 200
        };
        ToolTip.SetTip(nameLbl, r.Name);

        var typeLbl = BuildTypeBadge(r);
        typeLbl.CornerRadius = new CornerRadius(3);
        typeLbl.Padding = new Thickness(4, 1);
        typeLbl.Margin = new Thickness(4, 0);

        var cpuBar = _config.ShowBars
                        ? BuildInlineBar(r.CpuPct, r.CpuDisplay)
                        : new Border();

        var ramBar = _config.ShowBars
                        ? BuildInlineBar(r.MemoryPct, r.MemoryDisplay)
                        : new Border();

        var tagsPanel = _config.ShowTags
                            ? BuildTagsPanel(r.Tags)
                            : new WrapPanel();

        tagsPanel.VerticalAlignment = VerticalAlignment.Center;

        var btnPanel = new DockPanel
        {
            LastChildFill = false,
            VerticalAlignment = VerticalAlignment.Center
        };

        var rowGrid = new AGrid
        {
            VerticalAlignment = VerticalAlignment.Center,
            ColumnDefinitions = new ColumnDefinitions("44,50,200,90,180,200,200,96")
        };

        void SetCol(Control c, int col, HorizontalAlignment ha = HorizontalAlignment.Stretch)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            c.HorizontalAlignment = ha;
            c.Margin = new Thickness(0, 0, 8, 0);
            AGrid.SetColumn(c, col);
            rowGrid.Children.Add(c);
        }

        SetCol(statusDotPanel, 0, HorizontalAlignment.Left);
        SetCol(idLbl, 1);
        SetCol(nameLbl, 2);
        SetCol(typeLbl, 3, HorizontalAlignment.Center);
        SetCol(tagsPanel, 4);
        SetCol(cpuBar, 5);
        SetCol(ramBar, 6);
        SetCol(btnPanel, 7, HorizontalAlignment.Center);

        var hoverBrush = new SolidColorBrush(Color.FromArgb(18, 128, 128, 128));
        var row = new Border
        {
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 1),
            CornerRadius = new CornerRadius(6),
            Child = rowGrid
        };
        row.PointerEntered += (_, _) => row.Background = hoverBrush;
        row.PointerExited += (_, _) => row.Background = null;

        AddActionButtons(btnPanel, r, isCard: false);
        return row;
    }

    internal void RebuildListView(List<ResourceRow> rows)
    {
        _listContent.Children.Clear();

        ForEachNodeGroup(rows, (nodeName, nodeRow, nodeVms) =>
        {
            var header = BuildNodeHeader(nodeName, nodeRow);
            header.Margin = new Thickness(0, 0, 0, 2);

            var rowsPanel = new StackPanel { Spacing = 2 };
            foreach (var item in nodeVms)
            {
                rowsPanel.Children.Add(BuildListRow(item));
            }

            var group = new StackPanel { Spacing = 4 };
            group.Children.Add(header);
            group.Children.Add(BuildSectionDivider(0.1));
            group.Children.Add(rowsPanel);

            _listContent.Children.Add(group);
        });
    }
}
