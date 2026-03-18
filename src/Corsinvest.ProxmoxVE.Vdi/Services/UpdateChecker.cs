/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Semver;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class UpdateChecker
{
    private const string GitHubRepo = "Corsinvest/cv4pve-vdi";
    private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases";

    /// <summary>
    /// Checks GitHub for a newer release.
    /// If the current version is stable, only stable releases are considered.
    /// If the current version is a pre-release, pre-releases are included.
    /// Returns (newVersion, url) if a newer release is available, null otherwise.
    /// </summary>
    public static async Task<(string Version, string Url)?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var currentStr = ApplicationHelper.Version.TrimStart('v');
            if (string.IsNullOrEmpty(currentStr)) { return null; }

            if (!SemVersion.TryParse(currentStr, SemVersionStyles.Any, out var currentVer)) { return null; }

            var includePrerelease = currentVer.IsPrerelease;

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("cv4pve-vdi", ApplicationHelper.Version));
            http.Timeout = TimeSpan.FromSeconds(10);

            var releases = await http.GetFromJsonAsync<List<GitHubRelease>>(ApiUrl, ct);
            if (releases is null) { return null; }

            var candidate = releases
                .Where(r => !r.Draft && (includePrerelease || !r.Prerelease) && !string.IsNullOrEmpty(r.TagName))
                .Select(r => (Release: r, Ver: ParseVer(r.TagName!)))
                .Where(x => x.Ver is not null)
                .OrderByDescending(x => x.Ver!, SemVersion.PrecedenceComparer)
                .FirstOrDefault();

            if (candidate.Release is null) { return null; }

            if (currentVer.ComparePrecedenceTo(candidate.Ver!) < 0)
            {
                return (candidate.Release.TagName, candidate.Release.HtmlUrl ?? string.Empty);
            }
        }
        catch { /* ignore network errors */ }

        return null;
    }

    private static SemVersion? ParseVer(string tag)
        => SemVersion.TryParse(tag.TrimStart('v'), SemVersionStyles.Any, out var v) ? v : null;

    /// <summary>
    /// Runs CheckAsync at startup and then every 12 hours.
    /// Calls onNewVersion on the UI thread when a newer version is found.
    /// </summary>
    public static void StartBackground(Action<string, string> onNewVersion, CancellationToken ct = default)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await CheckAsync(ct);
                if (result.HasValue)
                {
                    await Dispatcher.UIThread.InvokeAsync(
                        () => onNewVersion(result.Value.Version, result.Value.Url));
                }

                await Task.Delay(TimeSpan.FromHours(12), ct).ContinueWith(_ => { });
            }
        }, ct);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
    }
}
