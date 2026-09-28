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
    /// Gets the first non-loopback IPv4 of a QEMU VM via guest agent.
    /// Returns null if agent is not available or no IP found.
    /// </summary>
    public static async Task<string?> GetVmIpAsync(PveClient client, string node, long vmId)
    {
        try
        {
            var ifaces = await client.Nodes[node].Qemu[vmId].Agent.NetworkGetInterfaces.GetAsync();
            return ifaces?.Result
                          .Where(i => !string.IsNullOrEmpty(i.HardwareAddress)
                                      && i.HardwareAddress != "00:00:00:00:00:00"
                                      && i.HardwareAddress != "0:0:0:0:0:0")
                          .SelectMany(i => i.IpAddresses)
                          .FirstOrDefault(a => a.IpAddressType == "ipv4" && !a.IpAddress.StartsWith("127."))
                          ?.IpAddress;
        }
        catch { return null; }
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
