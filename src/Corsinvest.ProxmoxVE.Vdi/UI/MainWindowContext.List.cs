/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindowContext
{
    private static Control BuildInlineBar(double pct, string text) => new StackPanel
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
                Text = text, FontSize = 10, Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        }
    };

    private Control BuildListRow(ResourceRow r)
    {
        var dot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(r.IsActive ? AppColors.Running : AppColors.Stopped),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var idLbl = new TextBlock { Text = r.IdDisplay, FontSize = 11, Opacity = 0.5, VerticalAlignment = VerticalAlignment.Center, Width = 50 };
        var nameLbl = new TextBlock { Text = r.Name, FontWeight = FontWeight.Medium, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Width = 200 };

        var typeLbl = new Border
        {
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1),
            Margin = new Thickness(4, 0),
            Background = new SolidColorBrush(AppColors.TypeBadge),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                        Data = Geometry.Parse(r.ResourceType == ClusterResourceType.Node ? Icons.Server : r.VmType == VmType.Qemu ? Icons.Vm : Icons.Ct),
                        Width = 10, Height = 10, Foreground = Brushes.White
                    },
                    new TextBlock
                    {
                        Text = r.ResourceType == ClusterResourceType.Node ? "Node" : r.VmType == VmType.Qemu ? "VM" : "CT",
                        FontSize = 10, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        var cpuBar = _config.ShowBars ? BuildInlineBar(r.CpuPct, r.CpuDisplay) : new Border();
        var ramBar = _config.ShowBars ? BuildInlineBar(r.MemoryPct, r.MemoryDisplay) : new Border();

        var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var tag in r.Tags)
        {
            tagsPanel.Children.Add(BuildTagBadge(tag));
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        AddActionButtons(btnPanel, r, isCard: false);

        // colonne fisse: dot(20) | id(50) | name(200) | type(52) | tags(180) | cpu(200) | ram(200) | buttons(96)
        var rowGrid = new AGrid
        {
            VerticalAlignment = VerticalAlignment.Center,
            ColumnDefinitions = new ColumnDefinitions("20,50,200,52,180,200,200,96")
        };

        void SetCol(Control c, int col, HorizontalAlignment ha = HorizontalAlignment.Stretch)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            c.HorizontalAlignment = ha;
            c.Margin = new Thickness(0, 0, 8, 0);
            AGrid.SetColumn(c, col);
            rowGrid.Children.Add(c);
        }

        SetCol(dot, 0, HorizontalAlignment.Center);
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
        return row;
    }

    internal void RebuildListView(List<ResourceRow> rows)
    {
        _listContent.Children.Clear();

        var allNodeNames = rows
            .Where(r => r.ResourceType == ClusterResourceType.Vm)
            .Select(r => r.NodeName)
            .Distinct()
            .Union(rows.Where(r => r.ResourceType == ClusterResourceType.Node).Select(r => r.Name))
            .OrderBy(n => n)
            .ToList();

        foreach (var nodeName in allNodeNames)
        {
            var nodeRow = _allRows.FirstOrDefault(r => r.ResourceType == ClusterResourceType.Node && r.Name == nodeName);
            var nodeVms = rows.Where(r => r.ResourceType == ClusterResourceType.Vm && r.NodeName == nodeName).ToList();
            if (nodeRow == null && nodeVms.Count == 0)
            {
                continue;
            }

            var dotColor = nodeRow?.IsActive == true ? AppColors.Running : AppColors.Stopped;
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 2) };
            header.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(dotColor), VerticalAlignment = VerticalAlignment.Center });
            header.Children.Add(new TextBlock { Text = nodeName, FontWeight = FontWeight.SemiBold, FontSize = 12, Opacity = 0.7 });

            var rowsPanel = new StackPanel { Spacing = 2 };
            foreach (var vm in nodeVms)
            {
                rowsPanel.Children.Add(BuildListRow(vm));
            }

            var group = new StackPanel { Spacing = 4 };
            group.Children.Add(header);
            group.Children.Add(new Border { Height = 1, Opacity = 0.1, Margin = new Thickness(0, 0, 0, 2) });
            group.Children.Add(rowsPanel);

            _listContent.Children.Add(group);
        }
    }
}
