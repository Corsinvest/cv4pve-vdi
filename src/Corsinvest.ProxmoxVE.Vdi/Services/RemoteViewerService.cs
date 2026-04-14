/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class RemoteViewerService
{
    public static async Task<string> LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, ClusterConfig host)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return "SPICE viewer path is not configured. Please set it in Settings → Viewer.";
        }

        var (error, fileName) = await RemoteViewerHelper.PrepareSpiceAsync(client, node, vmType, vmId, host.Spice.Proxy);
        if (error != null) { return error; }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        RemoteViewerHelper.Launch(config.ViewerPath, fileName!, viewerOptions, false);
        return string.Empty;
    }

    public static async Task<string> LaunchVncAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return "SPICE viewer path is not configured. Please set it in Settings → Viewer.";
        }

        var (error, fileName, bridge) = await RemoteViewerHelper.PrepareVncAsync(client, node, vmType, vmId);
        if (error != null) { return error; }

        _ = Task.Run(async () =>
        {
            await using (bridge)
            {
                RemoteViewerHelper.Launch(config.ViewerPath, fileName!, string.Empty, true);
            }
        });

        return string.Empty;
    }
}
