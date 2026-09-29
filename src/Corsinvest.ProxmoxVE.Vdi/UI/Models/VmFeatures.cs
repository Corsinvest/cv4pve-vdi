/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Models;

internal record VmFeatures(
    bool Spice,
    bool Audio,
    bool UsbRedirect,
    bool FolderSharing,
    int Monitors,
    bool AgentConfigured,
    bool? AgentRunning)
{
    public static readonly VmFeatures None = new(false, false, false, false, 0, false, null);
}
