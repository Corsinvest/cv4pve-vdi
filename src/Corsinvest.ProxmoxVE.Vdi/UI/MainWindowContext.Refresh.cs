/*
using static Corsinvest.ProxmoxVE.Api.Shared.Models.Vm.VmQemuAgentNetworkGetInterfaces;
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL; // required: partial class files don't share usings

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindowContext
{
    internal async Task RefreshAsync(Border? busyOverlay = null)
    {
        if (_firstLoad)
        {
            _lblStatus.Text = L("Loading");
            busyOverlay?.IsVisible = true;
        }
        else
        {
            _lblStatus.Text = L("Refreshing");
        }

        try
        {
            try { _tagColorMap = (await _client.Cluster.Options.GetAsync())?.TagStyle?.ColorMap ?? string.Empty; }
            catch { _tagColorMap = string.Empty; }

            if (_permissions.Count == 0)
            {
                try { _permissions = await _client.Access.Permissions.GetPermissionsAsync(); }
                catch { _permissions = new Dictionary<string, IReadOnlyList<string>>(); }
            }

            var resources = await _client.Cluster.Resources.GetAsync();

            var nodes = resources.Where(r => r.ResourceType == ClusterResourceType.Node)
                                 .OrderBy(r => r.Node)
                                 .ToList();

            var vms = resources.Where(r => r.ResourceType == ClusterResourceType.Vm && !r.IsUnknown)
                               .OrderBy(r => r.Node).ThenBy(r => r.VmId)
                               .ToList();

            // invalida cache per VM che non sono più running (RDP) o che sono tornate running (SPICE config)
            var runningIds = vms.Where(v => v.IsRunning).Select(v => v.VmId).ToHashSet();
            foreach (var id in _rdpCache.Keys.Where(id => !runningIds.Contains(id)).ToList())
                _rdpCache.Remove(id);
            foreach (var id in _spiceConfigCache.Keys.Where(id => runningIds.Contains(id)).ToList())
                _spiceConfigCache.Remove(id);

            // SPICE check — running: Status.Current, stopped: Config vga (cached) — 10 alla volta
            var spiceResults = new List<(long VmId, bool HasSpice)>();
            var spiceToCheck = vms.Where(v => v.VmType == VmType.Qemu && (v.IsRunning || !_spiceConfigCache.ContainsKey(v.VmId))).ToList();
            foreach (var chunk in spiceToCheck.Chunk(10))
            {
                var batch = await Task.WhenAll(chunk.Select(async v =>
                {
                    try
                    {
                        if (v.IsRunning)
                        {
                            var st = await _client.Nodes[v.Node].Qemu[v.VmId].Status.Current.GetAsync();
                            return (v.VmId, HasSpice: st?.Spice == true);
                        }
                        else
                        {
                            var cfg = await _client.Nodes[v.Node].Qemu[v.VmId].Config.GetAsync();
                            var vga = cfg?.Vga?.ToLowerInvariant() ?? string.Empty;
                            var hasSpice = vga.StartsWith("qxl") || vga == "spice";
                            _spiceConfigCache[v.VmId] = hasSpice;
                            return (v.VmId, HasSpice: hasSpice);
                        }
                    }
                    catch { return (v.VmId, HasSpice: false); }
                }));
                spiceResults.AddRange(batch);
            }
            // aggiungi risultati dalla cache per VM spente già controllate
            foreach (var kv in _spiceConfigCache.Where(kv => !spiceToCheck.Any(v => v.VmId == kv.Key)))
                spiceResults.Add((kv.Key, kv.Value));
            var spiceMap = spiceResults.ToDictionary(r => r.VmId, r => r.HasSpice);

            // RDP check — solo VM running non in cache — 10 alla volta
            var rdpToCheck = vms.Where(v => v.VmType == VmType.Qemu && v.IsRunning && !_rdpCache.ContainsKey(v.VmId)).ToList();
            var rdpResults = new List<(long VmId, bool HasRdp, string? Ip)>();
            foreach (var chunk in rdpToCheck.Chunk(10))
            {
                var batch = await Task.WhenAll(chunk.Select(async v =>
                {
                    var ip = await VmService.GetVmIpAsync(_client, v.Node, v.VmId);
                    if (ip is null) { return (v.VmId, HasRdp: false, Ip: (string?)null); }
                    var hasRdp = await VmService.IsRdpOpenAsync(ip);
                    var result = (v.VmId, HasRdp: hasRdp, Ip: hasRdp ? ip : null);
                    _rdpCache[v.VmId] = (hasRdp, hasRdp ? ip : null);
                    return result;
                }));
                rdpResults.AddRange(batch);
            }
            // aggiungi risultati dalla cache RDP
            foreach (var kv in _rdpCache.Where(kv => !rdpToCheck.Any(v => v.VmId == kv.Key)))
                rdpResults.Add((kv.Key, kv.Value.HasRdp, kv.Value.Ip));
            var rdpMap = rdpResults.ToDictionary(r => r.VmId, r => (r.HasRdp, r.Ip));

            _allRows.Clear();

            IEnumerable<string> EffectivePrivs(string path)
            {
                if (_permissions.TryGetValue("/", out var rootPrivs))
                {
                    foreach (var p in rootPrivs) { yield return p; }
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

            foreach (var item in nodes)
            {
                var privs = EffectivePrivs($"/nodes/{item.Node}").ToHashSet();
                _allRows.Add(new ResourceRow(item, false, false, null, false, privs.Contains("Sys.Console")));
            }

            foreach (var item in vms)
            {
                var privs = EffectivePrivs($"/vms/{item.VmId}").ToHashSet();
                var canPower = privs.Contains("VM.PowerMgmt");
                var canConsole = privs.Contains("VM.Console");

                var hasSpice = item.VmType == VmType.Lxc
                                ? (item.IsRunning && canConsole)
                                : spiceMap.GetValueOrDefault(item.VmId);

                var (hasRdp, rdpIp) = item.VmType == VmType.Qemu
                                        ? rdpMap.GetValueOrDefault(item.VmId)
                                        : (false, null);

                _allRows.Add(new ResourceRow(item, hasSpice, hasRdp, rdpIp, canPower, canConsole));
            }

            // update stats
            _lblRunning.Text = _allRows.Count(r => r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
            _lblStopped.Text = _allRows.Count(r => !r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
            _lblNodes.Text = nodes.Count.ToString();
            _lblVMs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Qemu).ToString();
            _lblCTs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Lxc).ToString();

            // rebuild node filter checkboxes
            var knownNodes = _nodeFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
            foreach (var item in nodes.Where(n => !knownNodes.Contains(n.Node)))
            {
                var chk = new CheckBox
                {
                    Tag = item.Node,
                    Content = Icons.WithText(Icons.Server, item.Node!),
                    IsChecked = true
                };

                chk.IsCheckedChanged += (_, _) =>
                {
                    if (chk.IsChecked == true)
                    {
                        _filterNodes.Remove(item.Node!);
                    }
                    else
                    {
                        _filterNodes.Add(item.Node!);
                    }

                    ApplyFilter();
                };
                _nodeFilters.Children.Add(chk);
            }

            // rebuild tag filter buttons
            var allTags = _allRows.SelectMany(r => r.Tags).Distinct().OrderBy(t => t).ToList();
            var existingTags = _tagFilters.Children.OfType<ToggleButton>().Select(b => b.Tag as string).ToHashSet();
            foreach (var tag in allTags.Where(t => !existingTags.Contains(t)))
            {
                var color = GetTagColor(tag);
                var tb = new ToggleButton
                {
                    Tag = tag,
                    Margin = new Thickness(0, 0, 4, 4),
                    Padding = new Thickness(6, 2),
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new Border
                            {
                                Width = 10,
                                Height = 10,
                                CornerRadius = new CornerRadius(2),
                                Background = new SolidColorBrush(color),
                                BorderBrush = ThemeBorderBrush(),
                                BorderThickness = new Thickness(1.5),
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = tag,
                                FontSize = 11,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    }
                };

                tb.IsCheckedChanged += (_, _) =>
                {
                    if (tb.IsChecked == true)
                    {
                        _filterTags.Add(tag);
                    }
                    else
                    {
                        _filterTags.Remove(tag);
                    }

                    ApplyFilter();
                };
                _tagFilters.Children.Add(tb);
            }

            ApplyFilter();
            _lblStatus.Text = $"{DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"{L("ErrorPrefix")}{ex.Message}";
        }
        finally
        {
            busyOverlay?.IsVisible = false;

            _firstLoad = false;
        }
    }

    internal async Task LaunchSpiceAsync(ResourceRow row)
    {
        string err;
        if (row.ResourceType == ClusterResourceType.Node)
        {
            _lblStatus.Text = $"{L("SpiceShellPrefix")}{row.Name}...";
            err = await RemoteViewerService.LaunchNodeSpiceAsync(_client, row.Resource.Node, _config, _host);
        }
        else
        {
            _lblStatus.Text = $"{L("SpicePrefix")}{row.Name}...";
            err = await RemoteViewerService.LaunchSpiceAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, _config, _host);
        }
        _lblStatus.Text = string.IsNullOrEmpty(err) ? L("SpiceLaunched") : $"{L("ErrorPrefix")}{err}";
    }

    internal async Task<bool> ConfirmAsync(string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = L("Confirm"),
            Width = 320,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnYes = new Button
        {
            Content = L("Yes"),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var btnNo = new Button
        {
            Content = L("No"),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        btnYes.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        btnNo.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 12,
                    Children = { btnNo, btnYes }
                }
            }
        };
        dlg.Closed += (_, _) => tcs.TrySetResult(false);
        await dlg.ShowDialog(_window!);
        return await tcs.Task;
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

        if (_chkRunning.IsChecked != true)
        {
            filtered = filtered.Where(r => !r.IsActive);
        }

        if (_chkStopped.IsChecked != true)
        {
            filtered = filtered.Where(r => r.IsActive);
        }

        filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                        || (r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Qemu && _chkQemu.IsChecked == true)
                                        || (r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Lxc && _chkLxc.IsChecked == true));

        if (_filterNodes.Count > 0)
        {
            filtered = filtered.Where(r =>
                !_filterNodes.Contains(r.NodeName) &&
                !(r.ResourceType == ClusterResourceType.Node && _filterNodes.Contains(r.Name)));
        }

        if (_filterTags.Count > 0)
        {
            filtered = filtered.Where(r => r.Tags.Any(t => _filterTags.Contains(t)));
        }

        var list = filtered.Where(r => r.HasAnyVdiAction).ToList();
        RebuildCardView(list);
        RebuildListView(list);
    }
}
