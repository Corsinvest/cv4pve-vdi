/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private StackPanel BuildMiniBar(string label, double pct, string text)
    {
        var header = new AGrid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        header.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
        }.Secondary(),
        0);

        header.Add(new TextBlock
        {
            Text = text,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        }.Secondary(),
        1);

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

    private Control BuildCard(ResourceRow r)
    {
        var dot = BuildStatusDot(r);
        var statusLbl = new TextBlock
        {
            Text = r.StatusDisplay,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

        var typeBadge = BuildTypeBadge(r);

        // top row: name(*) | badge(Auto)
        var nameRow = new AGrid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        nameRow.Add(new TextBlock
        {
            Text = r.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        }, 0);
        nameRow.Add(typeBadge, 1);

        // ID + Pool row below name
        Control idRow = new Border();
        if (!string.IsNullOrEmpty(r.IdDisplay))
        {
            var idText = string.IsNullOrEmpty(r.Pool)
                ? $"Id: {r.IdDisplay}"
                : $"Id: {r.IdDisplay}  ·  {r.Pool}";
            idRow = new TextBlock
            {
                Text = idText,
                FontSize = 11,
                Margin = new Thickness(0, 1, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            }.Secondary();
        }

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Children = { dot, statusLbl }
        };

        var tagsPanel = BuildTagsPanel(r.Tags);

        Control cpuSection = new Border();
        Control ramSection = new Border();
        if (_config.ShowBars && (r.ResourceType != ClusterResourceType.Node || r.IsActive))
        {
            cpuSection = BuildMiniBar("CPU", r.CpuPct, r.CpuDisplay);
            ramSection = BuildMiniBar("RAM", r.MemoryPct, r.MemoryDisplay);
        }

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        AddActionButtons(btnPanel, r, isCard: true);

        // Fixed-row grid for alignment across cards:
        // 0=name, 1=id, 2=status, 3=tags(*), 4=cpu, 5=ram, 6=buttons
        var bodyGrid = new AGrid();
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 0 name
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 1 id
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 2 status
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));  // 3 tags
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 4 cpu
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 5 ram
        bodyGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));  // 6 buttons

        nameRow.Margin = new Thickness(0, 0, 0, 4);
        idRow.Margin = new Thickness(0, 0, 0, 4);
        statusRow.Margin = new Thickness(0, 0, 0, 4);
        tagsPanel.Margin = new Thickness(0, 0, 0, 4);
        cpuSection.Margin = new Thickness(0, 0, 0, 2);

        bodyGrid.Add(nameRow, 0, 0);
        bodyGrid.Add(idRow, 0, 1);
        bodyGrid.Add(statusRow, 0, 2);
        bodyGrid.Add(tagsPanel, 0, 3);
        bodyGrid.Add(cpuSection, 0, 4);
        bodyGrid.Add(ramSection, 0, 5);

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
            bodyGrid.Add(btnSection, 0, 6);
        }

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
                    Property = Avalonia.Visual.RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(120)
                }
            },
            Child = bodyGrid
        };

        card.GetObservable(Avalonia.Input.InputElement.IsPointerOverProperty)
            .Subscribe(over => card.RenderTransform = new ScaleTransform(
                over
                    ? 1.025
                    : 1,
                over
                    ? 1.025
                    : 1));
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

    private void AddActionButtons(StackPanel panel, ResourceRow r, bool isCard)
    {
        var padding = isCard
                        ? new Thickness(8, 6)
                        : new Thickness(4, 2);

        if (_config.ShowStartButton && r.CanPower && !r.Resource.IsRunning)
        {
            var btn = new Button
            {
                Content = AppIcons.Row(AppIcons.Play, new SolidColorBrush(AppColors.Running)),
                Padding = padding
            };
            Avalonia.Controls.ToolTip.SetTip(btn, L("Start"));
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmStart && !await ConfirmAsync($"Start {r.Name}?"))
                {
                    return;
                }

                _lblStatus.Text = $"Starting {r.Name}...";
                await VmService.ChangeStatusAsync(_client, r.Resource.Node, r.Resource.VmId, r.VmType, VmStatus.Start);
                if (_btnAutoRef != null && _btnAutoRef.IsChecked != true)
                {
                    _btnAutoRef.IsChecked = true;
                }
                await RefreshAsync();
            };
            panel.Children.Add(btn);
        }

        if (_config.ShowShutdownButton && r.CanPower && r.Resource.IsRunning)
        {
            var btn = new Button
            {
                Content = AppIcons.Row(AppIcons.Stop, new SolidColorBrush(AppColors.Shutdown)),
                Padding = padding
            };
            Avalonia.Controls.ToolTip.SetTip(btn, L("Shutdown"));
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmShutdown && !await ConfirmAsync($"Shutdown {r.Name}?"))
                {
                    return;
                }

                _lblStatus.Text = $"Shutdown {r.Name}...";
                await VmService.ChangeStatusAsync(_client, r.Resource.Node, r.Resource.VmId, r.VmType, VmStatus.Shutdown);
                await RefreshAsync();
            };
            panel.Children.Add(btn);
        }

        if (r.CanSpice)
        {
            var btn = new Button
            {
                Content = AppIcons.Row(AppIcons.Spice),
                Padding = padding
            };
            Avalonia.Controls.ToolTip.SetTip(btn, L("Spice"));
            btn.Click += async (_, _) => await LaunchSpiceAsync(r);
            panel.Children.Add(btn);
        }

        if (r.HasRdp && r.RdpIp is not null)
        {
            var btn = new Button
            {
                Content = AppIcons.Row(AppIcons.Rdp),
                Padding = padding
            };
            Avalonia.Controls.ToolTip.SetTip(btn, $"RDP → {r.RdpIp}");
            btn.Click += (_, _) =>
            {
                var err = VmService.LaunchRdp(r.RdpIp, _config.RdpPath);
                _lblStatus.Text = string.IsNullOrEmpty(err)
                    ? $"RDP → {r.Name}"
                    : $"Error: {err}";
            };
            panel.Children.Add(btn);
        }
    }
}
