/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

/// <summary>
/// Mirrors the native <c>CRED_TYPE_*</c> constants from <c>advapi32.dll</c>.
/// </summary>
internal enum WindowsCredentialType : uint
{
    /// <summary>Generic credential (used by most apps, e.g. Git).</summary>
    Generic = 1,

    /// <summary>Domain password credential (required by mstsc for RDP SSO).</summary>
    DomainPassword = 2
}
