/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

/// <summary>
/// Configures the Windows Credential Manager (Vault) integration for a launcher.
/// When <see cref="Enable"/> is true, the launcher will temporarily inject
/// the user credentials into the Vault before starting the executable, and
/// remove them shortly after.
/// </summary>
internal sealed class WindowsCredentialDefinition
{
    /// <summary>Activates the Vault integration for this launcher.</summary>
    public bool Enable { get; set; }

    /// <summary>Vault entry type. <c>DomainPassword</c> is required by mstsc for RDP.</summary>
    public WindowsCredentialType Type { get; set; } = WindowsCredentialType.Generic;

    /// <summary>
    /// Target template written into the Vault. Supports the <c>{ip}</c> token.
    /// Example for RDP: <c>TERMSRV/{ip}</c>. Defaults to <c>{ip}</c>.
    /// </summary>
    public string Target { get; set; } = "{ip}";
}
