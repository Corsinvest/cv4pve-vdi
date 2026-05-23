/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
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

    /// <summary>
    /// The OS-level UI culture captured at startup. Used to restore "auto" mode after the
    /// user explicitly picked a language and then switched back.
    /// </summary>
    private static readonly CultureInfo _systemCulture = CultureInfo.CurrentUICulture;

    /// <summary>
    /// Last culture set via <see cref="ApplyLanguage"/>. Used by <see cref="ReapplyLanguage"/> to
    /// re-set <see cref="CultureInfo.CurrentUICulture"/> on the calling thread, recovering
    /// from cases where async/dispatcher continuations leave the thread on a different culture.
    /// </summary>
    private static CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    /// <summary>Returns the localized string for <paramref name="key"/>, falling back to the key itself.</summary>
    public static string L(string key) => _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>
    /// Applies the given language code as the current UI culture.
    /// <c>"auto"</c> or empty restores the OS culture captured at startup.
    /// Any other value (e.g. <c>"it"</c>, <c>"de"</c>, <c>"fr"</c>, <c>"es"</c>) overrides
    /// <see cref="CultureInfo.CurrentUICulture"/>. Invalid codes are silently ignored.
    /// </summary>
    public static void ApplyLanguage(string language)
    {
        CultureInfo culture;
        if (string.IsNullOrEmpty(language) || language == "auto")
        {
            culture = _systemCulture;
        }
        else
        {
            try { culture = new CultureInfo(language); }
            catch { return; /* invalid code — keep current culture */ }
        }

        // Set on the calling thread AND as default for any thread spawned later
        // (Avalonia dispatcher continuations, ContinueWith on the thread pool, etc.).
        // Without DefaultThreadCurrentUICulture, dialogs opened from async/dispatched
        // callbacks would fall back to the previous culture.
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        _currentCulture = culture;
    }

    /// <summary>
    /// Re-applies the last culture set via <see cref="ApplyLanguage"/> on the calling thread.
    /// Call as the first line of any window-builder method (e.g. <c>Create(...)</c>) so that
    /// inline <see cref="L"/> calls used to construct controls always see the chosen culture,
    /// even when the calling thread was left on a different culture by an earlier async or
    /// dispatcher continuation.
    /// </summary>
    public static void ReapplyLanguage()
    {
        CultureInfo.CurrentUICulture = _currentCulture;
        CultureInfo.CurrentCulture = _currentCulture;
    }
}
