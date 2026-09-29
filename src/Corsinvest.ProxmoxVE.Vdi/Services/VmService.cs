/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Net.Sockets;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class VmService
{
    public static async Task ChangeStatusAsync(PveClient client, string node, long vmId, VmType vmType, VmStatus status)
        => await VmHelper.ChangeStatusVmAsync(client, node, vmType, vmId, status);

    /// <summary>
    /// IPv4 address of a guest, for the services (RDP, SSH, ...).
    /// VMs: the interfaces reported by the QEMU guest agent. Containers: <c>/lxc/{vmid}/interfaces</c>
    /// (only <c>VM.Audit</c>, no agent). The interface whose MAC belongs to a NIC configured in Proxmox VE
    /// (<c>net0</c> first) wins: a guest running Docker also lists its bridges (172.17.0.1, ...), often
    /// before the real NIC. Without a match, the first non-loopback IPv4 with a real MAC.
    /// Null when nothing is found or the call fails.
    /// </summary>
    public static async Task<string?> GetGuestIpAsync(PveClient client, string node, long vmId, VmType vmType)
    {
        try
        {
            var nics = vmType == VmType.Lxc
                        ? await GetLxcInterfacesAsync(client, node, vmId)
                        : await GetQemuInterfacesAsync(client, node, vmId);

            nics = [.. nics.Where(a => !string.IsNullOrEmpty(a.Mac)
                                       && a.Mac.Replace(":", string.Empty).Trim('0').Length > 0)];

            Api.Shared.Models.Vm.VmConfig? config = vmType == VmType.Lxc
                                ? await client.Nodes[node].Lxc[vmId].Config.GetAsync()
                                : await client.Nodes[node].Qemu[vmId].Config.GetAsync();

            var configuredMacs = (config?.Networks ?? [])
                                    .OrderBy(a => a.Id)
                                    .Select(a => a.MacAddress)
                                    .Where(a => !string.IsNullOrEmpty(a))
                                    .ToList();

            foreach (var mac in configuredMacs)
            {
                var ip = nics.Where(a => string.Equals(a.Mac, mac, StringComparison.OrdinalIgnoreCase))
                             .SelectMany(a => a.Ipv4)
                             .FirstOrDefault(IsUsable);
                if (ip != null) { return ip; }
            }

            return nics.SelectMany(a => a.Ipv4).FirstOrDefault(IsUsable);
        }
        catch { return null; }

        static bool IsUsable(string ip) => !string.IsNullOrEmpty(ip) && !ip.StartsWith("127.");
    }

    private static async Task<List<(string Mac, List<string> Ipv4)>> GetQemuInterfacesAsync(PveClient client, string node, long vmId)
    {
        var ifaces = await client.Nodes[node].Qemu[vmId].Agent.NetworkGetInterfaces.GetAsync();
        return [.. (ifaces?.Result ?? []).Select(a => (a.HardwareAddress ?? string.Empty,
                                                        a.IpAddresses?.Where(b => b.IpAddressType == "ipv4")
                                                                      .Select(b => b.IpAddress)
                                                                      .ToList() ?? []))];
    }

    private static async Task<List<(string Mac, List<string> Ipv4)>> GetLxcInterfacesAsync(PveClient client, string node, long vmId)
    {
        var result = await client.Nodes[node].Lxc[vmId].Interfaces.Ip();
        var list = new List<(string Mac, List<string> Ipv4)>();
        if (!result.IsSuccessStatusCode) { return list; }

        foreach (var item in result.Response.data)
        {
            // "inet" is "10.0.0.5/24": present on every PVE version that has this endpoint.
            var inet = ((IDictionary<string, object>)item).TryGetValue("inet", out var value)
                        ? value?.ToString() ?? string.Empty
                        : string.Empty;
            var ip = inet.Split('/')[0];
            list.Add((((IDictionary<string, object>)item).TryGetValue("hwaddr", out var hw) ? hw?.ToString() ?? string.Empty : string.Empty,
                      string.IsNullOrEmpty(ip) ? [] : [ip]));
        }

        return list;
    }

    /// <summary>
    /// Scans ports for all platform launchers that have a DefaultPort and are not already configured.
    /// Returns the launchers whose port responded within the timeout.
    /// </summary>
    public static async Task<IReadOnlyList<LauncherDefinition>> DiscoverServicesAsync(
        string ip,
        IEnumerable<LauncherDefinition> launchers,
        IEnumerable<VmServiceConfig> existing,
        int timeoutMs = 500)
    {
        var existingIds = existing.Select(s => s.ServiceId).ToHashSet();

        var candidates = launchers
            .Where(l => l.DefaultPort > 0 && !existingIds.Contains(l.ServiceId))
            .ToList();

        var tasks = candidates.Select(async l =>
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(ip, l.DefaultPort).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
                return (l, reachable: true);
            }
            catch { return (l, reachable: false); }
        });

        var results = await Task.WhenAll(tasks);
        return [.. results.Where(r => r.reachable).Select(r => r.l)];
    }
}
