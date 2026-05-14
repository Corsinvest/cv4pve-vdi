/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private static IBrush ThemeBorderBrush() => AppColors.BorderBrush(AppColors.IsDark);

    internal void BuildAgentBadge(ResourceRow row, StackPanel parent)
    {
        if (!row.Features.AgentConfigured)
        {
            return;
        }

        string tooltip;
        Color color;
        double opacity;

        if (!_config.EnableAgentPing || row.Features.AgentRunning is null)
        {
            tooltip = L("BadgeAgentUnknown");
            color = Colors.Gray;
            opacity = 0.5;
        }
        else if (row.Features.AgentRunning is true)
        {
            tooltip = L("BadgeAgentRunning");
            color = AppColors.Running;
            opacity = 0.9;
        }
        else
        {
            tooltip = L("BadgeAgentStopped");
            color = AppColors.Shutdown;
            opacity = 0.9;
        }

        var icon = new PathIcon
        {
            Data = Geometry.Parse(AppIcons.Agent),
            Width = 11,
            Height = 11,
            Foreground = new SolidColorBrush(color),
            Opacity = opacity,
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(icon, tooltip);
        parent.Children.Add(icon);
    }

    internal static Control BuildFeatureBadges(ResourceRow row)
    {
        if (!row.Features.Audio && !row.Features.UsbRedirect && !row.Features.Clipboard)
        {
            return new Border();
        }

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        void AddBadge(string icon, string tooltip, Color color, double opacity = 0.9)
        {
            var pathIcon = new PathIcon
            {
                Data = Geometry.Parse(icon),
                Width = 12,
                Height = 12,
                Foreground = new SolidColorBrush(color),
                Opacity = opacity
            };
            ToolTip.SetTip(pathIcon, tooltip);
            panel.Children.Add(pathIcon);
        }

        if (row.Features.Audio) { AddBadge(AppIcons.Audio, L("BadgeAudioSpice"), AppColors.Running); }
        if (row.Features.UsbRedirect) { AddBadge(AppIcons.Usb, L("BadgeUsbRedirect"), AppColors.Running); }
        if (row.Features.Clipboard) { AddBadge(AppIcons.Clipboard2, L("BadgeClipboard"), AppColors.Running); }

        return panel;
    }


    internal void AddActionButtons(DockPanel panel, ResourceRow row, bool isCard)
    {
        var padding = isCard ? new Thickness(8, 6) : new Thickness(4, 2);
        var spacing = isCard ? 6 : 4;

        void AddLeft(Button btn)
        {
            btn.Margin = new Thickness(0, 0, spacing, 0);
            DockPanel.SetDock(btn, Dock.Left);
            panel.Children.Add(btn);
        }

        if (_config.ShowStartButton && row.CanPower && !row.Resource.IsRunning)
        {
            var btn = UiHelper.IconButton(AppIcons.Play, "Start", padding, size: 14, foregroundColor: AppColors.Running);
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmStart && !await DialogHelper.ConfirmAsync(_window!, string.Format(L("ConfirmStart"), row.Name))) { return; }
                await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Start);
                if (_btnAutoRef?.IsChecked is not true) { _btnAutoRef!.IsChecked = true; }
                await RefreshAsync();
            };
            AddLeft(btn);
        }

        if (_config.ShowShutdownButton && row.CanPower && row.Resource.IsRunning)
        {
            var btn = UiHelper.IconButton(AppIcons.Stop, "Shutdown", padding, size: 14, foregroundColor: AppColors.Shutdown);
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmShutdown && !await DialogHelper.ConfirmAsync(_window!, string.Format(L("ConfirmShutdown"), row.Name))) { return; }
                await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Shutdown);
                if (_btnAutoRef?.IsChecked is not true) { _btnAutoRef!.IsChecked = true; }
                await RefreshAsync();
            };
            AddLeft(btn);
        }

        if (row.ResourceType != ClusterResourceType.Node)
        {
            AddLeft(BuildConnectButton(row, padding));
        }
    }

    private Button BuildConnectButton(ResourceRow row, Thickness padding)
    {
        var menu = new ContextMenu();

        // SPICE and VNC as first entries
        if (row.CanSpice)
        {
            var item = new MenuItem { Header = UiHelper.WithText(AppIcons.Spice, L("Spice")) };
            item.Click += async (_, _) => await LaunchSpiceAsync(row);
            menu.Items.Add(item);
        }

        if (_config.EnableVnc && row.CanVnc)
        {
            var item = new MenuItem { Header = UiHelper.WithText(AppIcons.Vnc, L("Vnc")) };
            item.Click += async (_, _) => await LaunchVncAsync(row);
            menu.Items.Add(item);
        }

        // Custom services
        var vmConfig = _host.Vms.FirstOrDefault(v => v.VmId == (int)row.Resource.VmId);
        var services = vmConfig?.Services ?? [];
        if (services.Count > 0)
        {
            var launchers = LauncherEngine.LoadForCurrentPlatform(AppConfigManager.LaunchersUserFile);

            if (menu.Items.Count > 0) { menu.Items.Add(new Separator()); }

            foreach (var svc in services)
            {
                var launcher = launchers.FirstOrDefault(l => l.ServiceId == svc.ServiceId);
                if (launcher is null) { continue; }

                var svcCopy = svc;
                var launcherCopy = launcher;
                var item = new MenuItem { Header = launcher.DisplayName };
                item.Click += async (_, _) =>
                {
                    var ip = !string.IsNullOrEmpty(svcCopy.IpOverride)
                                ? svcCopy.IpOverride
                                : await VmService.GetVmIpAsync(_client, row.Resource.Node, row.Resource.VmId);

                    if (string.IsNullOrEmpty(ip))
                    {
                        ShowToast(L("NoIpAvailable"), NotificationSeverity.Warning);
                        return;
                    }

                    var creds = svcCopy.CredentialSource switch
                    {
                        CredentialSource.Vdi => new Credentials { Username = vdiUser, Password = vdiPassword },
                        CredentialSource.Manual => svcCopy.Credentials,
                        _ => null
                    };

                    var extraArgs = string.IsNullOrEmpty(svcCopy.ExtraArgs) ? launcherCopy.ExtraArgs : svcCopy.ExtraArgs;
                    var err = LauncherEngine.Launch(launcherCopy, ip, svcCopy.Port, creds, extraArgs);
                    if (!string.IsNullOrEmpty(err)) { ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error); }
                };
                menu.Items.Add(item);
            }
        }

        // "Configure services..." — always last, always present.
        // In kiosk mode it requires the admin password (configuration is an admin task).
        if (menu.Items.Count > 0) { menu.Items.Add(new Separator()); }
        var itemConfigure = new MenuItem { Header = UiHelper.WithText(AppIcons.Settings, L("ConfigureServices")) };
        itemConfigure.Click += async (_, _) =>
        {
            if (!await KioskGuard.CheckAsync(_window!, _config)) { return; }

            var vmId = (int)row.Resource.VmId;
            var vmConfig = _host.Vms.FirstOrDefault(v => v.VmId == vmId) ?? new Config.Models.VmConfig { VmId = vmId };
            var updated = await VmServicesWindow.ShowAsync(_window!, vmConfig, row.Name, AppConfigManager.LaunchersUserFile, _client, row.Resource.Node);
            var existing = _host.Vms.FindIndex(v => v.VmId == vmId);
            if (existing >= 0) { _host.Vms[existing] = updated; }
            else { _host.Vms.Add(updated); }
            AppConfigManager.Save(_config);
            ApplyFilter();
        };
        menu.Items.Add(itemConfigure);

        var btnConnect = UiHelper.IconButton(AppIcons.Network, "Connect", padding, size: 14);
        btnConnect.Click += (_, _) => menu.Open(btnConnect);
        return btnConnect;
    }

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
        return Color.FromRgb((byte)(h & 0xFF), (byte)((h >> 8) & 0xFF), (byte)(((h >> 16) & 0x7F) | 0x40));
    }

    private static Ellipse BuildStatusDot(ResourceRow row)
        => new()
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(row.IsActive
                    ? AppColors.Running
                    : AppColors.Stopped),
            VerticalAlignment = VerticalAlignment.Center
        };

    private static Border BuildTypeBadge(ResourceRow row)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PathIcon
                {
                    Data = Geometry.Parse(row.ResourceType == ClusterResourceType.Node
                                            ? AppIcons.Server
                                            : row.VmType == VmType.Qemu
                                                ? AppIcons.Vm
                                                : AppIcons.Ct),
                    Width = 12,
                    Height = 12,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = row.ResourceType == ClusterResourceType.Node
                            ? L("TypeNode")
                            : row.VmType == VmType.Qemu
                                ? L("TypeVm")
                                : L("TypeCt"),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        if (row.ResourceType != ClusterResourceType.Node && !string.IsNullOrEmpty(row.OsType))
        {
            stack.Children.Add(new PathIcon
            {
                Data = Geometry.Parse(row.OsType.StartsWith("win")
                        ? AppIcons.Windows
                        : AppIcons.Linux),
                Width = 12,
                Height = 12,
                Foreground = new SolidColorBrush(AppColors.OsBrushColor(row.OsType)),
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
        static Color Blend(Color c, Color target, double t)
            => Color.FromArgb(255,
                              (byte)(c.R + ((target.R - c.R) * t)),
                              (byte)(c.G + ((target.G - c.G) * t)),
                              (byte)(c.B + ((target.B - c.B) * t)));

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

    private static StackPanel BuildNodeHeader(string nodeName, ResourceRow? nodeRow)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

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
        => [.. rows.Where(r => r.ResourceType == ClusterResourceType.Vm)
               .Select(r => r.NodeName)
               .Distinct()
               .Union(rows.Where(r => r.ResourceType == ClusterResourceType.Node).Select(r => r.Name))
               .Order()];

    internal IEnumerable<string> EffectivePrivs(string path)
    {
        if (_permissions.TryGetValue("/", out var rootPrivs))
        {
            foreach (var item in rootPrivs)
            {
                yield return item;
            }
        }

        var current = string.Empty;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current += "/" + part;
            if (_permissions.TryGetValue(current, out var privs))
            {
                foreach (var item in privs)
                {
                    yield return item;
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
            filtered = filtered.Where(a => a.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || a.IdDisplay.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || a.Description.Contains(ft, StringComparison.OrdinalIgnoreCase));
        }

        var filterByStatus = _chkRunning.IsChecked is true || _chkStopped.IsChecked is true;
        if (filterByStatus)
        {
            filtered = filtered.Where(a => a.ResourceType == ClusterResourceType.Node
                                            || (_chkRunning.IsChecked is true && a.IsActive)
                                            || (_chkStopped.IsChecked is true && !a.IsActive));
        }

        var filterByType = _chkQemu.IsChecked is true || _chkLxc.IsChecked is true;
        if (filterByType)
        {
            filtered = filtered.Where(a => a.ResourceType == ClusterResourceType.Node
                                            || (a.VmType == VmType.Qemu && _chkQemu.IsChecked is true)
                                            || (a.VmType == VmType.Lxc && _chkLxc.IsChecked is true));
        }

        if (_filterNodes.Count > 0)
        {
            filtered = filtered.Where(a => a.ResourceType == ClusterResourceType.Node
                                            ? _filterNodes.Contains(a.Name)
                                            : _filterNodes.Contains(a.NodeName));
        }

        if (_filterPools.Count > 0)
        {
            filtered = filtered.Where(a => a.ResourceType == ClusterResourceType.Node
                                            || _filterPools.Contains(a.Pool));
        }

        if (_filterTags.Count > 0)
        {
            filtered = filtered.Where(a => a.Tags.Length == 0
                                            || a.Tags.Any(t => _filterTags.Contains(t)));
        }

        var list = filtered.Where(a => a.HasAnyVdiAction).ToList();
        RebuildCardView(list);
        RebuildListView(list);
    }
}
