/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    internal async Task RefreshAsync()
    {
        if (_isRefreshing) { return; }

        _isRefreshing = true;

        _btnRefresh?.IsEnabled = false;
        _btnAutoRef?.IsEnabled = false;

        _progressBar.IsVisible = true;
        _progressBar.Value = 0;

        try
        {
            // 1. tag color map + permissions
            try { _tagColorMap = (await _client.Cluster.Options.GetAsync())?.TagStyle?.ColorMap ?? string.Empty; }
            catch { _tagColorMap = string.Empty; }

            if (_permissions.Count == 0)
            {
                try { _permissions = await _client.Access.Permissions.GetPermissionsAsync(); }
                catch { _permissions = new Dictionary<string, IReadOnlyList<string>>(); }
            }

            _progressBar.Value = 10;

            // 2. cluster resources
            try { _pveVersion = (await _client.Version.GetAsync())?.Version ?? string.Empty; } catch { }
            var resources = await _client.Cluster.Resources.GetAsync();

            var nodes = resources.Where(r => r.ResourceType == ClusterResourceType.Node)
                                 .OrderBy(r => r.Node)
                                 .ToList();

            var vms = resources.Where(r => r.ResourceType == ClusterResourceType.Vm && !r.IsUnknown)
                               .OrderBy(r => r.Node).ThenBy(r => r.VmId)
                               .ToList();

            _progressBar.Value = 20;

            // invalidate SPICE config cache for running VMs — status.current is the live source of truth
            var runningIds = vms.Where(v => v.IsRunning).Select(v => v.VmId).ToHashSet();
            foreach (var id in _spiceConfigCache.Keys.Where(id => runningIds.Contains(id)).ToList())
            {
                _spiceConfigCache.Remove(id);
            }

            _allRows.Clear();

            // 3. Nodes + LXC — shown immediately, no extra API calls
            foreach (var item in nodes)
            {
                var privs = EffectivePrivs($"/nodes/{item.Node}").ToHashSet();
                _allRows.Add(new ResourceRow(item, false, false, privs.Contains("Sys.Console"), string.Empty));
            }

            foreach (var item in vms.Where(v => v.VmType == VmType.Lxc).ToList())
            {
                var privs = EffectivePrivs($"/vms/{item.VmId}").ToHashSet();
                var canPower = privs.Contains("VM.PowerMgmt");
                var canConsole = privs.Contains("VM.Console");
                var hasSpice = _config.EnableSpice && item.IsRunning && canConsole;
                _allRows.Add(new ResourceRow(item, hasSpice, canPower, canConsole, "linux"));
            }

            // rebuild node filter checkboxes
            void ToggleFilter(HashSet<string> set, string value, bool isChecked)
            {
                if (isChecked)
                {
                    set.Add(value);
                }
                else
                {
                    set.Remove(value);
                }

                ApplyFilter();
            }

            var knownNodes = _nodeFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
            foreach (var item in nodes.Where(n => !knownNodes.Contains(n.Node)))
            {
                var chk = new CheckBox
                {
                    Tag = item.Node,
                    Content = item.Node,
                    IsChecked = false
                };
                chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterNodes, item.Node!, chk.IsChecked is true);
                _nodeFilters.Children.Add(chk);
            }

            UpdateStats(nodes.Count);
            ApplyFilter();
            _progressBar.Value = 30;

            // 4. QEMU — parallel chunks of 5, progressive
            var qemuVms = vms.Where(v => v.VmType == VmType.Qemu).ToList();
            // SPICE detection requires API calls; VNC is always available (no API needed)
            var qemuToCheck = _config.EnableSpice
                                ? qemuVms.Where(v => v.IsRunning || !_spiceConfigCache.ContainsKey(v.VmId)).ToList()
                                : [];
            var totalQemu = qemuToCheck.Count;
            var doneQemu = 0;

            // add all QEMU as placeholders first, SPICE resolved progressively
            foreach (var item in qemuVms)
            {
                var privs = EffectivePrivs($"/vms/{item.VmId}").ToHashSet();
                var canPower = privs.Contains("VM.PowerMgmt");
                var canConsole = privs.Contains("VM.Console");
                var osType = _osTypeCache.GetValueOrDefault(item.VmId, string.Empty);
                var hasSpice = _config.EnableSpice && _spiceConfigCache.GetValueOrDefault(item.VmId, false);
                var features = _featuresCache.GetValueOrDefault(item.VmId, VmFeatures.None);
                _allRows.Add(new ResourceRow(item, hasSpice, canPower, canConsole, osType, features));
            }

            UpdateStats(nodes.Count);
            ApplyFilter();

            foreach (var chunk in qemuToCheck.Chunk(5))
            {
                await Task.WhenAll(chunk.Select(async v =>
                {
                    try
                    {
                        bool hasSpice;
                        string osType;
                        VmFeatures features;

                        if (v.IsRunning)
                        {
                            var vm = _client.Nodes[v.Node].Qemu[v.VmId];

                            var status = await vm.Status.Current.GetAsync();
                            hasSpice = status?.Spice is true;
                            var agentRunning = await GetAgentRunningAsync(vm, v.VmId);

                            if (_osTypeCache.TryGetValue(v.VmId, out var value))
                            {
                                osType = value;
                                var cached = _featuresCache.GetValueOrDefault(v.VmId, VmFeatures.None);
                                features = cached with { AgentRunning = agentRunning };
                            }
                            else
                            {
                                var cfg = await vm.Config.GetAsync();
                                osType = cfg?.OsType?.ToLowerInvariant() ?? string.Empty;
                                _osTypeCache[v.VmId] = osType;
                                features = BuildFeatures(cfg, agentRunning);
                                _featuresCache[v.VmId] = features;
                            }
                        }
                        else
                        {
                            var cfg = await _client.Nodes[v.Node].Qemu[v.VmId].Config.GetAsync();
                            var vga = cfg?.Vga?.ToLowerInvariant() ?? string.Empty;
                            hasSpice = vga.StartsWith("qxl") || vga == "spice";
                            osType = cfg?.OsType?.ToLowerInvariant() ?? string.Empty;
                            _spiceConfigCache[v.VmId] = hasSpice;
                            _osTypeCache[v.VmId] = osType;
                            features = BuildFeatures(cfg, false);
                            _featuresCache[v.VmId] = features;
                        }

                        // update existing row in _allRows
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var idx = _allRows.FindIndex(r => r.Resource.VmId == v.VmId);
                            if (idx >= 0)
                            {
                                var old = _allRows[idx];
                                _allRows[idx] = new ResourceRow(v,
                                                                hasSpice,
                                                                old.CanPower,
                                                                old.CanConsole,
                                                                osType,
                                                                features);
                            }
                        });
                    }
                    catch { }
                }));

                doneQemu += chunk.Length;
                _progressBar.Value = 30 + (doneQemu * 50 / Math.Max(totalQemu, 1));
                UpdateStats(nodes.Count);
                ApplyFilter();
            }

            _progressBar.Value = 80;

            // rebuild pool filter — only if enabled and only pools of VDI-actionable VMs
            if (_config.ShowPools)
            {
                var allPools = _allRows
                    .Where(r => r.HasAnyVdiAction && !string.IsNullOrEmpty(r.Pool))
                    .Select(r => r.Pool)
                    .Distinct()
                    .Order()
                    .ToList();
                var knownPools = _poolFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
                foreach (var pool in allPools.Where(p => !knownPools.Contains(p)))
                {
                    var chk = new CheckBox
                    {
                        Tag = pool,
                        Content = pool,
                        IsChecked = false
                    };
                    chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterPools, pool, chk.IsChecked is true);
                    _poolFilters.Children.Add(chk);
                }
            }

            // rebuild tag filters — only if enabled and only tags of VDI-actionable VMs
            if (_config.ShowTags)
            {
                var allTags = _allRows.Where(r => r.HasAnyVdiAction).SelectMany(r => r.Tags).Distinct().Order().ToList();
                var existingTags = _tagFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
                foreach (var tag in allTags.Where(t => !existingTags.Contains(t)))
                {
                    var chk = new CheckBox
                    {
                        Tag = tag,
                        Content = tag,
                        IsChecked = false
                    };
                    chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterTags, tag, chk.IsChecked is true);
                    _tagFilters.Children.Add(chk);
                }
            }

            ApplyFilter();
            _btnRefresh?.IsEnabled = true;
            _btnAutoRef?.IsEnabled = true;
            _progressBar.Value = 100;
        }
        catch (Exception ex)
        {
            ShowToast($"{L("ErrorPrefix")}{ex.Message}", NotificationSeverity.Error);
        }
        finally
        {
            _progressBar.IsVisible = false;
            _isRefreshing = false;
            _btnRefresh?.IsEnabled = true;
            _btnAutoRef?.IsEnabled = true;
        }
    }

    internal async Task LaunchVncAsync(ResourceRow row)
    {
        var err = await RemoteViewerService.LaunchVncAsync(_client,
                                                           row.Resource.Node,
                                                           row.Resource.VmId,
                                                           row.VmType,
                                                           _config);
        if (!string.IsNullOrEmpty(err))
        {
            ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error);
        }
    }

    internal async Task LaunchSpiceAsync(ResourceRow row)
    {
        var err = await RemoteViewerService.LaunchSpiceAsync(_client,
                                                             row.Resource.Node,
                                                             row.Resource.VmId,
                                                             row.VmType,
                                                             _config,
                                                             _host);
        if (!string.IsNullOrEmpty(err))
        {
            ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error);
        }
    }

    private async Task<bool?> GetAgentRunningAsync(dynamic vm, long vmId)
    {
        if (!_config.EnableAgentPing) { return null; }

        if (_agentPingCache.TryGetValue(vmId, out var pingEntry)
            && (DateTime.Now - pingEntry.CheckedAt).TotalSeconds < AgentPingCacheSeconds)
        {
            return pingEntry.Running;
        }

        var timeout = Task.Delay(AgentPingTimeoutMs);
        Task<Api.Result> pingTask = vm.Agent.Ping.Ping();
        var completed = await Task.WhenAny(pingTask, timeout);
        var running = completed != timeout && pingTask.Result?.IsSuccessStatusCode is true;
        _agentPingCache[vmId] = (running, DateTime.Now);
        return running;
    }

    private static VmFeatures BuildFeatures(VmConfigQemu? cfg, bool? agentRunning)
    {
        if (cfg is null) { return VmFeatures.None; }

        var audio = cfg.Audio0?.Contains("driver=spice") is true;
        var usbRedirect = cfg.ExtensionData
                             ?.Where(kv => UsbRegex().IsMatch(kv.Key))
                             .Any(kv => kv.Value?.ToString()?.Contains("host=spice") is true) is true;

        var clipboard = cfg.SpiceEnhancements?.Contains("clipboard") is true;
        return new VmFeatures(audio, usbRedirect, cfg.AgentEnabled, agentRunning, clipboard);
    }

    [GeneratedRegex(@"^usb\d+$")]
    private static partial Regex UsbRegex();
}
