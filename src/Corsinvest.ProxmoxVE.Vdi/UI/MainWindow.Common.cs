/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private static IBrush ThemeBorderBrush() => AppColors.BorderBrush(AppColors.IsDark);

    /// <summary>
    /// Small rounded label, the shape of the VM/CT badge and the tags: the same neutral grey background
    /// and border for every pill, content in the tint colour (or the theme colour when
    /// <paramref name="tintContent"/> is false).
    /// </summary>
    private static Border BuildPill(Color tint, Control content, string? tooltip, bool tintContent = true)
    {
        if (tintContent)
        {
            switch (content)
            {
                case TextBlock text: text.Foreground = new SolidColorBrush(PillTint(tint)); break;
                case PathIcon icon: icon.Foreground = new SolidColorBrush(PillTint(tint)); break;
            }
        }

        var pill = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1),
            Background = new SolidColorBrush(Color.FromArgb(40, AppColors.TypeBadge.R, AppColors.TypeBadge.G, AppColors.TypeBadge.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(110, AppColors.TypeBadge.R, AppColors.TypeBadge.G, AppColors.TypeBadge.B)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = content
        };
        if (!string.IsNullOrEmpty(tooltip)) { ToolTip.SetTip(pill, tooltip); }
        return pill;
    }

    /// <summary>
    /// Softer shades of the status colours for the pill icons, readable on the grey pill in both themes;
    /// other colours are returned unchanged.
    /// </summary>
    private static Color PillTint(Color color)
        => color == AppColors.Running ? Color.Parse(AppColors.IsDark ? "#4ade80" : "#16a34a")
            : color == AppColors.Shutdown ? Color.Parse(AppColors.IsDark ? "#f87171" : "#dc2626")
            : color == AppColors.BarMedium ? Color.Parse(AppColors.IsDark ? "#fbbf24" : "#d97706")
            : color;

    private static PathIcon PillIcon(string iconData)
        => new()
        {
            Data = Geometry.Parse(iconData),
            Width = 11,
            Height = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

    private static TextBlock PillText(string text)
        => new()
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

    /// <summary>
    /// Status of the guest as a coloured icon pill — running green, paused amber, stopped grey — with the
    /// state in the tooltip, so status, agent and SPICE fit on one row of the card.
    /// </summary>
    internal static Border BuildStatusPill(ResourceRow row)
    {
        var (icon, text, color) = row.Resource.IsRunning
                                    ? (AppIcons.Play, L("StateRunning"), AppColors.Running)
                                    : row.Resource.IsPaused
                                        ? (AppIcons.Pause, L("StatePaused"), AppColors.BarMedium)
                                        : (AppIcons.Stop, L("StateStopped"), AppColors.Stopped);
        return BuildPill(color, PillIcon(icon), text);
    }

    /// <summary>
    /// QEMU guest agent as a pill, only for VMs with the agent enabled in their options: green when it
    /// answers the ping, red when it does not, grey when the ping is off or the VM is stopped.
    /// </summary>
    internal Border? BuildAgentPill(ResourceRow row)
    {
        if (!row.Features.AgentConfigured) { return null; }

        var (tooltip, color) = !row.IsActive
                                    ? (L("BadgeAgentVmStopped"), AppColors.Stopped)
                                    : !_config.EnableAgentPing || row.Features.AgentRunning is null
                                        ? (L("BadgeAgentUnknown"), AppColors.Stopped)
                                        : row.Features.AgentRunning is true
                                            ? (L("BadgeAgentRunning"), AppColors.Running)
                                            : (L("BadgeAgentStopped"), AppColors.Shutdown);

        return BuildPill(color, PillIcon(AppIcons.Agent), tooltip);
    }

    /// <summary>
    /// Pill for VMs with a SPICE display, in the style of the VM/CT badge but lighter, with an icon for
    /// each SPICE feature configured (audio, USB redirect, folder sharing, monitors) and the list in the
    /// tooltip. Null when the VM has no SPICE display.
    /// </summary>
    internal static Border? BuildSpicePill(ResourceRow row)
    {
        var f = row.Features;
        if (!f.Spice) { return null; }

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                PillText("SPICE")
            }
        };

        var tooltip = new List<string> { L("BadgeSpice") };

        void AddFeature(string icon, string text)
        {
            panel.Children.Add(PillIcon(icon));
            tooltip.Add(text);
        }

        if (f.Audio) { AddFeature(AppIcons.Audio, L("BadgeAudioSpice")); }
        if (f.UsbRedirect) { AddFeature(AppIcons.Usb, L("BadgeUsbRedirect")); }
        if (f.FolderSharing) { AddFeature(AppIcons.Folder, L("BadgeFolderSharing")); }
        if (f.Monitors > 1)
        {
            AddFeature(AppIcons.Monitor, string.Format(L("BadgeMonitors"), f.Monitors));
            panel.Children.Add(new TextBlock
            {
                Text = f.Monitors.ToString(),
                FontSize = 10,
                Margin = new Thickness(-2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return BuildPill(AppColors.TypeBadge, panel, string.Join(Environment.NewLine, tooltip), tintContent: false);
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
                try
                {
                    await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Start);
                    if (_btnAutoRef is { IsChecked: false }) { _btnAutoRef.IsChecked = true; }
                    await RefreshAsync();
                }
                catch (Exception ex) { ShowToast($"{L("ErrorPrefix")}{ex.Message}", NotificationSeverity.Error); }
            };
            AddLeft(btn);
        }

        if (_config.ShowShutdownButton && row.CanPower && row.Resource.IsRunning)
        {
            var btn = UiHelper.IconButton(AppIcons.Stop, "Shutdown", padding, size: 14, foregroundColor: AppColors.Shutdown);
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmShutdown && !await DialogHelper.ConfirmAsync(_window!, string.Format(L("ConfirmShutdown"), row.Name))) { return; }
                try
                {
                    await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Shutdown);
                    if (_btnAutoRef is { IsChecked: false }) { _btnAutoRef.IsChecked = true; }
                    await RefreshAsync();
                }
                catch (Exception ex) { ShowToast($"{L("ErrorPrefix")}{ex.Message}", NotificationSeverity.Error); }
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
            var launchers = PlatformLaunchers;

            if (menu.Items.Count > 0) { menu.Items.Add(new Separator()); }

            foreach (var svc in services)
            {
                var launcher = launchers.FirstOrDefault(l => l.ServiceId == svc.ServiceId);
                if (launcher is null) { continue; }

                var svcCopy = svc;
                var launcherCopy = launcher;
                var item = new MenuItem { Header = UiHelper.WithText(AppIcons.ForLauncher(launcher.Icon), launcher.DisplayName) };
                item.Click += async (_, _) =>
                {
                    var ip = !string.IsNullOrEmpty(svcCopy.IpOverride)
                                ? svcCopy.IpOverride
                                : await VmService.GetGuestIpAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType);

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
                    var (err, p) = LauncherEngine.Launch(launcherCopy, ip, svcCopy.Port, creds, extraArgs);
                    if (!string.IsNullOrEmpty(err)) { ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error); return; }
                    _sessions.Register(p,
                                       row.Resource.VmId,
                                       row.Name,
                                       launcherCopy.DisplayName,
                                       AppIcons.ForLauncher(launcherCopy.Icon),
                                       row.OsType);
                };
                menu.Items.Add(item);
            }
        }

        // "Configure services...": always last, always present.
        // In kiosk mode it requires the admin password (configuration is an admin task).
        if (menu.Items.Count > 0) { menu.Items.Add(new Separator()); }
        var itemConfigure = new MenuItem { Header = UiHelper.WithText(AppIcons.Settings, L("ConfigureServices")) };
        itemConfigure.Click += async (_, _) =>
        {
            if (!await KioskGuard.CheckAsync(_window!, _config)) { return; }

            var vmId = (int)row.Resource.VmId;
            var vmConfig = _host.Vms.FirstOrDefault(v => v.VmId == vmId) ?? new Config.Models.VmConfig { VmId = vmId };
            var updated = await VmServicesWindow.ShowAsync(_window!, vmConfig, row.Name, AppConfigManager.LaunchersUserFile, _client, row.Resource.Node, row.VmType);
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

    /// <summary>
    /// Returns the OS glyph (Windows / Linux) tinted with the OS-specific colour
    /// used across the app. Single source of truth for both the type badge on the
    /// VM card and the pill in the Open sessions panel.
    /// </summary>
    internal static PathIcon BuildOsIcon(string osType) => new()
    {
        Data = Geometry.Parse(osType.StartsWith("win") ? AppIcons.Windows : AppIcons.Linux),
        Width = 12,
        Height = 12,
        Foreground = new SolidColorBrush(AppColors.OsBrushColor(osType)),
        VerticalAlignment = VerticalAlignment.Center
    };

    /// <summary>
    /// 12x12 inline glyph used by chips/pills/badges (e.g. SPICE, VNC, RDP markers
    /// on a session pill). Picks up the default foreground from the theme so it
    /// stays readable in both light and dark mode.
    /// </summary>
    internal static PathIcon BuildServiceIcon(string iconData) => new()
    {
        Data = Geometry.Parse(iconData),
        Width = 12,
        Height = 12,
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
            var osIcon = BuildOsIcon(row.OsType);
            osIcon.Margin = new Thickness(2, 0, 0, 0);
            stack.Children.Add(osIcon);
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
            // A node header without guests is noise: with a filter active it is every node the filter emptied.
            if (nodeVms.Count == 0)
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

    /// <summary>
    /// Privileges on <paramref name="path"/>. /access/permissions already returns, for each path, the
    /// privileges in effect there (inheritance and pools resolved, a more specific ACL replacing the
    /// inherited one), so the most specific listed path wins: adding up the parents would grant what a
    /// narrower role takes away.
    /// </summary>
    internal IEnumerable<string> EffectivePrivs(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = parts.Length; i >= 0; i--)
        {
            var current = "/" + string.Join('/', parts.Take(i));
            if (_permissions.TryGetValue(current, out var privs)) { return privs; }
        }

        return [];
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
        if (_suspendFilter) { return; }

        var filtered = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            var ft = _filterText.Trim().ToLowerInvariant();
            filtered = filtered.Where(a => a.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || a.IdDisplay.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || a.Description.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || a.Tags.Any(t => t.Contains(ft, StringComparison.OrdinalIgnoreCase)));
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
            filtered = filtered.Where(a => a.ResourceType == ClusterResourceType.Node
                                            || a.Tags.Any(t => _filterTags.Contains(t)));
        }

        // A stopped VM only survived the list filter via HasAnyVdiAction, which for a
        // stopped VM depends solely on its VGA being SPICE-capable (qxl/spice). A stopped
        // VNC-only VM (e.g. vga: virtio) therefore vanished from the list while stopped
        // SPICE VMs stayed visible, unless the "Stopped" status filter was ticked.
        //
        // Always include stopped-but-powerable rows so the Start button is reachable and
        // behaviour is consistent across VGA types. Status filters still remove these when
        // "Running" is selected.
        var list = filtered.Where(a => a.HasAnyVdiAction
                                       || (a.CanPower && !a.IsActive)).ToList();

        // Sort VMs/CTs globally per the user's choice (nodes keep their alphabetical order
        // applied at fetch time). _allRows is built in two passes (LXC first, then QEMU),
        // so without this re-sort the rendered order would always be CT-then-VM regardless
        // of ID/Name. Sorting here also makes the order observable in both grouped and
        // flat (GroupByNode = false) layouts.
        var sortedVms = _config.SortBy == AppConfig.SortByName
                            ? list.Where(r => r.ResourceType == ClusterResourceType.Vm)
                                  .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                            : list.Where(r => r.ResourceType == ClusterResourceType.Vm)
                                  .OrderBy(r => r.Resource.VmId);
        list = [.. list.Where(r => r.ResourceType == ClusterResourceType.Node), .. sortedVms];

        RebuildCardView(list);
        RebuildListView(list);

        // Node rows always pass the filters: the list is "empty" when no guest is left.
        _emptyState.IsVisible = !list.Any(r => r.ResourceType == ClusterResourceType.Vm);
    }
}
