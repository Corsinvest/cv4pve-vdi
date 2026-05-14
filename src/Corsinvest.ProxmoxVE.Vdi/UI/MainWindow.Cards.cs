/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static StackPanel BuildMiniBar(string label, double pct, string text)
    {
        var header = new AGrid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        header.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
        }.Secondary(), 0);

        header.Add(new TextBlock
        {
            Text = text,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        }.Secondary(), 1);

        return new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(0, 2),
            Children =
            {
                header,
                new ProgressBar
                {
                    Value = pct,
                    Maximum = 100,
                    Minimum = 0,
                    Height = 4,
                    Foreground = new SolidColorBrush(AppColors.BarColor(pct))
                }
            }
        };
    }

    private Control BuildCard(ResourceRow row)
    {
        // top row: name(*) | badge(Auto)
        var nameRow = new AGrid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        nameRow.Add(new TextBlock
        {
            Text = row.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        }, 0);
        nameRow.Add(BuildTypeBadge(row), 1);

        // ID + Pool row below name
        Control idRow = string.IsNullOrEmpty(row.IdDisplay)
                        ? new Border()
                        : new TextBlock
                        {
                            Text = _config.ShowPools && !string.IsNullOrEmpty(row.Pool)
                                        ? $"Id: {row.IdDisplay}  ·  {row.Pool}"
                                        : $"Id: {row.IdDisplay}",
                            FontSize = 11,
                            Margin = new Thickness(0, 1, 0, 0),
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }.Secondary();

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Children =
            {
                BuildStatusDot(row),
                new TextBlock
                {
                    Text = row.StatusDisplay,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        BuildAgentBadge(row, statusRow);

        Control tagsPanel = _config.ShowTags
                                ? BuildTagsPanel(row.Tags)
                                : new Border();

        var featureBadges = BuildFeatureBadges(row);

        Control cpuSection = new Border();
        Control ramSection = new Border();

        if (_config.ShowBars && (row.ResourceType != ClusterResourceType.Node || row.IsActive))
        {
            cpuSection = BuildMiniBar("CPU", row.CpuPct, row.CpuDisplay);
            ramSection = BuildMiniBar("RAM", row.MemoryPct, row.MemoryDisplay);
        }

        var btnPanel = new DockPanel
        {
            LastChildFill = false
        };

        // Fixed-row grid for alignment across cards:
        // 0=name, 1=id, 2=status, 3=features, 4=tags(*), 5=cpu, 6=ram, 7=buttons
        var bodyGrid = new AGrid();
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 0 name
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 1 id
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 2 status
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 3 features
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));  // 4 tags
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 5 cpu
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 6 ram
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 7 buttons

        nameRow.Margin = new Thickness(0, 0, 0, 4);
        idRow.Margin = new Thickness(0, 0, 0, 4);
        statusRow.Margin = new Thickness(0, 0, 0, 4);
        featureBadges.Margin = new Thickness(0, 0, 0, 4);
        tagsPanel.Margin = new Thickness(0, 0, 0, 4);
        cpuSection.Margin = new Thickness(0, 0, 0, 2);

        bodyGrid.Add(nameRow, 0, 0);
        bodyGrid.Add(idRow, 0, 1);
        bodyGrid.Add(statusRow, 0, 2);
        bodyGrid.Add(featureBadges, 0, 3);
        bodyGrid.Add(tagsPanel, 0, 4);
        bodyGrid.Add(cpuSection, 0, 5);
        bodyGrid.Add(ramSection, 0, 6);

        var card = new Border
        {
            Width = 240,
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = ThemeBorderBrush(),
            RenderTransform = new ScaleTransform(1, 1),
            RenderTransformOrigin = RelativePoint.Center,
            Transitions = new Transitions
            {
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(120)
                }
            },
            Child = bodyGrid
        };

        AddActionButtons(btnPanel, row, isCard: true);

        if (btnPanel.Children.Count > 0)
        {
            var btnSection = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 8, 0, 0),
                Children =
                {
                    new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        Opacity = 0.15,
                        Margin = new Thickness(0, 0, 0, 6)
                    },
                    btnPanel
                }
            };
            bodyGrid.Add(btnSection, 0, 7);
        }

        card.PointerEntered += (_, _) => card.RenderTransform = new ScaleTransform(1.025, 1.025);
        card.PointerExited += (_, _) => card.RenderTransform = new ScaleTransform(1, 1);
        return card;
    }

    internal void RebuildCardView(List<ResourceRow> rows)
    {
        _cardContent.Children.Clear();

        ForEachNodeGroup(rows, (nodeName, nodeRow, nodeVms) =>
        {
            var header = BuildNodeHeader(nodeName, nodeRow);
            header.Margin = new Thickness(0, 0, 0, 4);

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemSpacing = 12,
                LineSpacing = 12
            };
            foreach (var item in nodeVms)
            {
                wrap.Children.Add(BuildCard(item));
            }

            var group = new StackPanel { Spacing = 10 };
            group.Children.Add(header);
            group.Children.Add(BuildSectionDivider());
            if (wrap.Children.Count > 0)
            {
                group.Children.Add(wrap);
            }

            _cardContent.Children.Add(group);
        });

        var standaloneNodes = rows.Where(r => r.ResourceType == ClusterResourceType.Node).ToList();
        if (standaloneNodes.Count > 0 && _cardContent.Children.Count == 0)
        {
            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemSpacing = 12,
                LineSpacing = 12
            };
            foreach (var item in standaloneNodes)
            {
                wrap.Children.Add(BuildCard(item));
            }
            _cardContent.Children.Add(wrap);
        }
    }
}
