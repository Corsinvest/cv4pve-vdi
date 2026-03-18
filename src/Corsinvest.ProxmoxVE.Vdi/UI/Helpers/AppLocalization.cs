/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Resources;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

/// <summary>
/// Localization helper. Usage: AppLocalization.L("Key") or with 'using static': L("Key")
/// Add Strings.{culture}.resx files alongside Strings.resx for translations.
/// </summary>
internal static class AppLocalization
{
    private static readonly ResourceManager _rm = new("Corsinvest.ProxmoxVE.Vdi.UI.Resources.Strings",
                                                      typeof(AppLocalization).Assembly);

    /// <summary>Returns the localized string for <paramref name="key"/>, falling back to the key itself.</summary>
    public static string L(string key) => _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
