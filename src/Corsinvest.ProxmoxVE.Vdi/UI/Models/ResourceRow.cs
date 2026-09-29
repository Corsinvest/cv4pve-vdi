/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Models;

internal class ResourceRow(ClusterResource resource,
                           bool hasSpice,
                           bool canPower,
                           bool canConsole,
                           string osType,
                           VmFeatures? features = null)
{
    public ClusterResource Resource => resource;
    public ClusterResourceType ResourceType => resource.ResourceType;
    public VmType VmType => resource.VmType;

    public string IdDisplay
        => resource.ResourceType == ClusterResourceType.Node
            ? string.Empty
            : resource.VmId.ToString();

    public string Name
        => resource.ResourceType == ClusterResourceType.Node
            ? resource.Node ?? string.Empty
            : resource.Name ?? string.Empty;

    public string Description => resource.Description ?? string.Empty;
    public string Pool => resource.Pool ?? string.Empty;

    public string NodeName
        => resource.ResourceType == ClusterResourceType.Node
            ? string.Empty
            : resource.Node ?? string.Empty;

    public bool IsActive
        => resource.ResourceType == ClusterResourceType.Node
            ? resource.IsOnline
            : resource.IsRunning;

    public bool CanSpice
        => resource.ResourceType == ClusterResourceType.Node
            ? (resource.IsOnline && canConsole)
            : (resource.IsRunning && hasSpice && canConsole);

    public bool CanVnc
        => resource.ResourceType != ClusterResourceType.Node
            && resource.IsRunning && canConsole;

    public bool CanPower => canPower;
    public bool CanConsole => canConsole;

    public string OsType
        => resource.VmType == VmType.Lxc
        ? "linux"
        : osType;

    public VmFeatures Features => features ?? VmFeatures.None;

    public bool HasAnyVdiAction => ResourceType == ClusterResourceType.Node || hasSpice || CanVnc;

    public double CpuPct => resource.CpuUsagePercentage;
    public string CpuDisplay => FormatHelper.CpuInfo(resource.CpuUsagePercentage / 100.0, resource.CpuSize);

    public double MemoryPct
        => resource.MemorySize == 0
            ? 0
            : (double)resource.MemoryUsage / resource.MemorySize * 100.0;

    public string MemoryDisplay => FormatHelper.UsageInfo(resource.MemoryUsage, resource.MemorySize);

    public string[] Tags
        => string.IsNullOrWhiteSpace(resource.Tags)
            ? []
            : resource.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
