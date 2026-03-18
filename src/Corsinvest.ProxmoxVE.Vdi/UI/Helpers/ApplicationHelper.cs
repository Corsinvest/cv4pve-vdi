/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class ApplicationHelper
{
    public static readonly string Version =
        Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "?.?.?";
}
