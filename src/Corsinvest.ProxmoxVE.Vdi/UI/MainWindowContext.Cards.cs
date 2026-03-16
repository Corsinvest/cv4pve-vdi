/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL;
using AGrid = Avalonia.Controls.Grid;
using ABorder = Avalonia.Controls.Border;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindowContext
{
    private Color GetTagColor(string tag)
    {
        foreach (var entry in _tagColorMap.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length >= 3 && string.Equals(parts[0], tag, StringComparison.OrdinalIgnoreCase))
            {
                var hex = parts[2].TrimStart('#');
                try { return Color.Parse("#" + hex); } catch { }
            }
        }
        var h = Math.Abs(tag.GetHashCode());
        return Color.FromRgb((byte)(h & 0xFF), (byte)((h >> 8) & 0xFF), (byte)((h >> 16) & 0x7F | 0x40));
    }

    private Control BuildTagBadge(string tag)
    {
        var color = GetTagColor(tag);
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Border
                {
                    Width = 10, Height = 10,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(color),
                    BorderBrush = ThemeBorderBrush(),
                    BorderThickness = new Thickness(1.5),
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock { Text = tag, FontSize = 10, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    private static Control BuildMiniBar(string label, double pct, string text)
    {
        var bar = new ProgressBar
        {
            Value = pct,
            Maximum = 100,
            Minimum = 0,
            Height = 4,
            Foreground = new SolidColorBrush(AppColors.BarColor(pct))
        };
        var header = new AGrid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var lbl = new TextBlock { Text = label, FontSize = 10, Opacity = 0.5 };
        var val = new TextBlock { Text = text, FontSize = 10, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Right };
        AGrid.SetColumn(lbl, 0); AGrid.SetColumn(val, 1);
        header.Children.Add(lbl); header.Children.Add(val);
        return new StackPanel { Spacing = 2, Margin = new Thickness(0, 2), Children = { header, bar } };
    }

    private Control BuildCard(ResourceRow r)
    {
        var dot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(r.IsActive ? AppColors.Running : AppColors.Stopped),
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusLbl = new TextBlock { Text = r.StatusDisplay, FontSize = 11, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };

        var typeBadge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1),
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
                        Text = r.ResourceType == ClusterResourceType.Node ? L("TypeNode") : r.VmType == VmType.Qemu ? L("TypeVm") : L("TypeCt"),
                        FontSize = 10, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        var nameRow = new AGrid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        nameRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var nameLbl = new TextBlock { Text = r.Name, FontWeight = FontWeight.SemiBold, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        AGrid.SetColumn(nameLbl, 0); AGrid.SetColumn(typeBadge, 1);
        nameRow.Children.Add(nameLbl); nameRow.Children.Add(typeBadge);

        var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Children = { dot, statusLbl } };
        if (!string.IsNullOrEmpty(r.IdDisplay))
        {
            statusRow.Children.Add(new TextBlock { Text = $"· {r.IdDisplay}", FontSize = 11, Opacity = 0.5, VerticalAlignment = VerticalAlignment.Center });
        }

        var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var tag in r.Tags)
        {
            tagsPanel.Children.Add(BuildTagBadge(tag));
        }

        Control cpuSection = new Border();
        Control ramSection = new Border();
        if (_config.ShowBars && (r.ResourceType != ClusterResourceType.Node || r.IsActive))
        {
            cpuSection = BuildMiniBar("CPU", r.CpuPct, r.CpuDisplay);
            ramSection = BuildMiniBar("RAM", r.MemoryPct, r.MemoryDisplay);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        AddActionButtons(btnPanel, r, isCard: true);

        var body = new StackPanel { Spacing = 6, Children = { nameRow, statusRow, tagsPanel, cpuSection, ramSection } };
        if (btnPanel.Children.Count > 0)
        {
            body.Children.Add(new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Opacity = 0.15,
                Margin = new Thickness(0, 4, 0, 0)
            });
            body.Children.Add(btnPanel);
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
                    Property = ABorder.RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(120)
                }
            },
            Child = body
        };
        card.GetObservable(Avalonia.Input.InputElement.IsPointerOverProperty)
            .Subscribe(over => card.RenderTransform = new ScaleTransform(over ? 1.025 : 1, over ? 1.025 : 1));
        return card;
    }

    internal void RebuildCardView(List<ResourceRow> rows)
    {
        _cardContent.Children.Clear();

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
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 4) };

            header.Children.Add(new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(dotColor),
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = nodeName,
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
                Opacity = 0.7
            });

            if (nodeRow != null)
            {
                header.Children.Add(new TextBlock { Text = $"— {nodeRow.CpuDisplay}  {nodeRow.MemoryDisplay}", FontSize = 11, Opacity = 0.45, VerticalAlignment = VerticalAlignment.Center });
            }

            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 12, LineSpacing = 12 };
            foreach (var vm in nodeVms)
            {
                wrap.Children.Add(BuildCard(vm));
            }

            var group = new StackPanel { Spacing = 10 };
            group.Children.Add(header);
            group.Children.Add(new Border { Height = 1, Opacity = 0.12, Margin = new Thickness(0, 2, 0, 0) });
            if (wrap.Children.Count > 0)
            {
                group.Children.Add(wrap);
            }

            _cardContent.Children.Add(group);
        }

        var standaloneNodes = rows.Where(r => r.ResourceType == ClusterResourceType.Node).ToList();
        if (standaloneNodes.Count > 0 && _cardContent.Children.Count == 0)
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 12, LineSpacing = 12 };
            foreach (var n in standaloneNodes)
            {
                wrap.Children.Add(BuildCard(n));
            }

            _cardContent.Children.Add(wrap);
        }
    }

    private void AddActionButtons(StackPanel panel, ResourceRow r, bool isCard)
    {
        var padding = isCard ? new Thickness(8, 6) : new Thickness(4, 2);

        if (_config.ShowStartButton && r.CanPower && !r.Resource.IsRunning)
        {
            var btn = new Button { Content = Icons.Row(Icons.Play), Padding = padding };
            Avalonia.Controls.ToolTip.SetTip(btn, L("Start"));
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmStart && !await ConfirmAsync($"Start {r.Name}?"))
                {
                    return;
                }

                _lblStatus.Text = $"Starting {r.Name}...";
                await VmService.ChangeStatusAsync(_client, r.Resource.Node, r.Resource.VmId, r.VmType, VmStatus.Start);
                await RefreshAsync();
            };
            panel.Children.Add(btn);
        }

        if (_config.ShowShutdownButton && r.CanPower && r.Resource.IsRunning)
        {
            var btn = new Button { Content = Icons.Row(Icons.Stop), Padding = padding };
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
            var btn = new Button { Content = Icons.Row(Icons.Spice), Padding = padding };
            Avalonia.Controls.ToolTip.SetTip(btn, L("Spice"));
            btn.Click += async (_, _) => await LaunchSpiceAsync(r);
            panel.Children.Add(btn);
        }

        if (r.HasRdp && r.RdpIp is not null)
        {
            var btn = new Button { Content = Icons.Row(Icons.Windows), Padding = padding };
            Avalonia.Controls.ToolTip.SetTip(btn, $"RDP → {r.RdpIp}");
            btn.Click += (_, _) =>
            {
                var err = VmService.LaunchRdp(r.RdpIp, _config.RdpPath);
                _lblStatus.Text = string.IsNullOrEmpty(err) ? $"RDP → {r.Name}" : $"Error: {err}";
            };
            panel.Children.Add(btn);
        }
    }
}
