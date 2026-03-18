/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static IBrush ThemeBorderBrush() => AppColors.BorderBrush(AppColors.IsDark);

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

    private static Ellipse BuildStatusDot(ResourceRow r) => new()
    {
        Width = 8,
        Height = 8,
        Fill = new SolidColorBrush(r.IsActive
            ? AppColors.Running
            : AppColors.Stopped),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border BuildTypeBadge(ResourceRow r)
    {
        var iconData = r.ResourceType == ClusterResourceType.Node
                     ? AppIcons.Server
                     : r.VmType == VmType.Qemu
                         ? AppIcons.Vm
                         : AppIcons.Ct;

        var label = r.ResourceType == ClusterResourceType.Node
                  ? L("TypeNode")
                  : r.VmType == VmType.Qemu
                      ? L("TypeVm")
                      : L("TypeCt");

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PathIcon
                {
                    Data = Geometry.Parse(iconData),
                    Width = 12,
                    Height = 12,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        if (r.ResourceType != ClusterResourceType.Node && !string.IsNullOrEmpty(r.OsType))
        {
            stack.Children.Add(new PathIcon
            {
                Data = Geometry.Parse(r.OsType.StartsWith("win")
                    ? AppIcons.Windows
                    : AppIcons.Linux),
                Width = 12,
                Height = 12,
                Foreground = new SolidColorBrush(AppColors.OsBrushColor(r.OsType)),
                Margin = new Thickness(2, 0, 0, 0)
            });
        }

        return new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1),
            Background = new SolidColorBrush(AppColors.TypeBadge),
            Child = stack
        };
    }

    private WrapPanel BuildTagsPanel(IEnumerable<string> tags)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var tag in tags)
        {
            panel.Children.Add(BuildTagBadge(tag));
        }

        return panel;
    }

    private Border BuildTagBadge(string tag)
    {
        var color = GetTagColor(tag);
        // dark: blend toward white; light: blend toward black
        static Color Blend(Color c, Color target, double t) => Color.FromArgb(255,
            (byte)(c.R + (target.R - c.R) * t),
            (byte)(c.G + (target.G - c.G) * t),
            (byte)(c.B + (target.B - c.B) * t));
        var textColor = AppColors.IsDark
            ? Blend(color, Colors.White, 0.6)
            : Blend(color, Colors.Black, 0.5);
        return new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(7, 2),
            Margin = new Thickness(0, 0, 4, 2),
            Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(180, textColor.R, textColor.G, textColor.B)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = tag,
                FontSize = 10,
                Foreground = new SolidColorBrush(textColor),
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private StackPanel BuildNodeHeader(string nodeName, ResourceRow? nodeRow)
    {
        var dotColor = nodeRow?.IsActive == true
            ? AppColors.Running
            : AppColors.Stopped;
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
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
            FontSize = 13
        }.Secondary());

        if (nodeRow != null)
        {
            header.Children.Add(new TextBlock
            {
                Text = $"{nodeRow.CpuDisplay}  {nodeRow.MemoryDisplay}",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            }.Secondary());
        }
        return header;
    }

    private static Border BuildSectionDivider(double opacity = 0.12)
        => new()
        {
            Height = 1,
            Opacity = opacity,
            Margin = new Thickness(0, 2, 0, 0)
        };

    // Iterates node+vm groups and calls the callback to build each group's content
    private void ForEachNodeGroup(List<ResourceRow> rows, Action<string, ResourceRow?, List<ResourceRow>> callback)
    {
        foreach (var nodeName in GetOrderedNodeNames(rows))
        {
            var nodeRow = _allRows.FirstOrDefault(r => r.ResourceType == ClusterResourceType.Node && r.Name == nodeName);
            var nodeVms = rows.Where(r => r.ResourceType == ClusterResourceType.Vm && r.NodeName == nodeName).ToList();
            if (nodeRow == null && nodeVms.Count == 0)
            {
                continue;
            }
            callback(nodeName, nodeRow, nodeVms);
        }
    }

    private static List<string> GetOrderedNodeNames(List<ResourceRow> rows)
        => rows.Where(r => r.ResourceType == ClusterResourceType.Vm)
               .Select(r => r.NodeName)
               .Distinct()
               .Union(rows.Where(r => r.ResourceType == ClusterResourceType.Node).Select(r => r.Name))
               .OrderBy(n => n)
               .ToList();

    internal IEnumerable<string> EffectivePrivs(string path)
    {
        if (_permissions.TryGetValue("/", out var rootPrivs))
        {
            foreach (var p in rootPrivs)
            {
                yield return p;
            }
        }

        var current = "";
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current += "/" + part;
            if (_permissions.TryGetValue(current, out var privs))
            {
                foreach (var p in privs)
                {
                    yield return p;
                }
            }
        }
    }

    internal void UpdateStats(int nodeCount)
    {
        _lblRunning.Text = _allRows.Count(r => r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
        _lblStopped.Text = _allRows.Count(r => !r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
        _lblNodes.Text = nodeCount.ToString();
        _lblVMs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Qemu).ToString();
        _lblCTs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Lxc).ToString();
    }

    internal void ApplyFilter()
    {
        var filtered = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            var ft = _filterText.Trim().ToLowerInvariant();
            filtered = filtered.Where(r => r.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || r.IdDisplay.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || r.Description.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || r.Tags.Any(t => t.Contains(ft, StringComparison.OrdinalIgnoreCase)));
        }

        var filterByStatus = _chkRunning.IsChecked == true || _chkStopped.IsChecked == true;
        if (filterByStatus)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || (_chkRunning.IsChecked == true && r.IsActive)
                                            || (_chkStopped.IsChecked == true && !r.IsActive));
        }

        var filterByType = _chkQemu.IsChecked == true || _chkLxc.IsChecked == true;
        if (filterByType)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || (r.VmType == VmType.Qemu && _chkQemu.IsChecked == true)
                                            || (r.VmType == VmType.Lxc && _chkLxc.IsChecked == true));
        }

        if (_filterNodes.Count > 0)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            ? _filterNodes.Contains(r.Name)
                                            : _filterNodes.Contains(r.NodeName));
        }

        if (_filterPools.Count > 0)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || _filterPools.Contains(r.Pool));
        }

        if (_filterTags.Count > 0)
        {
            filtered = filtered.Where(r => r.Tags.Length == 0
                                            || r.Tags.Any(t => _filterTags.Contains(t)));
        }

        var list = filtered.Where(r => r.HasAnyVdiAction).ToList();
        RebuildCardView(list);
        RebuildListView(list);
    }
}
