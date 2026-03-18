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
                           bool hasRdp,
                           string? rdpIp,
                           bool canPower,
                           bool canConsole,
                           string osType)
{
    public ClusterResource Resource => resource;
    public ClusterResourceType ResourceType => resource.ResourceType;
    public VmType VmType => resource.VmType;

    public string IdDisplay => resource.ResourceType == ClusterResourceType.Node
        ? ""
        : resource.VmId.ToString();

    public string Name => resource.ResourceType == ClusterResourceType.Node
        ? resource.Node ?? ""
        : resource.Name ?? "";

    public string Description => resource.Description ?? "";
    public string Pool => resource.Pool ?? "";
    public string NodeName => resource.ResourceType == ClusterResourceType.Node
        ? ""
        : resource.Node ?? "";

    public bool IsActive => resource.ResourceType == ClusterResourceType.Node
        ? resource.IsOnline
        : resource.IsRunning;

    public bool CanSpice => resource.ResourceType == ClusterResourceType.Node
        ? (resource.IsOnline && canConsole)
        : (resource.IsRunning && hasSpice && canConsole);

    public bool HasRdp => hasRdp;
    public string? RdpIp => rdpIp;
    public bool CanPower => canPower;
    public bool CanConsole => canConsole;
    public string OsType => resource.VmType == VmType.Lxc
        ? "linux"
        : osType;

    public bool HasAnyVdiAction => ResourceType == ClusterResourceType.Node || hasSpice || hasRdp;

    public string StatusDisplay
        => resource.ResourceType == ClusterResourceType.Node
            ? (resource.IsOnline
                ? "Online"
                : "Offline")
        : (resource.IsRunning
            ? "Running"
            : resource.IsPaused
                ? "Paused"
                : "Stopped");

    public double CpuPct => resource.CpuUsagePercentage;
    public string CpuDisplay => FormatHelper.CpuInfo(resource.CpuUsagePercentage / 100.0, resource.CpuSize);
    public double MemoryPct => resource.MemorySize == 0
        ? 0
        : (double)resource.MemoryUsage / resource.MemorySize * 100.0;
    public string MemoryDisplay => FormatHelper.UsageInfo(resource.MemoryUsage, resource.MemorySize);

    public string[] Tags => string.IsNullOrWhiteSpace(resource.Tags)
        ? []
        : resource.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
