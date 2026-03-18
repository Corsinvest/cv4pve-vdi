/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Config;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static partial class RemoteViewerService
{
    [GeneratedRegex("^(http|https|)://.*$")]
    private static partial Regex ProxyUrlRegex();

    public static async Task<string> LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, VdiConfig config, VdiHost host)
    {
        var proxy = string.IsNullOrEmpty(host.Spice.Proxy)
            ? client.Host
            : host.Spice.Proxy;

        var (success, reasonPhrase, content) = vmType == VmType.Lxc
                                                ? await client.Nodes[node].Lxc[vmId].Spiceproxy.GetSpiceFileVVAsync(proxy)
                                                : await client.Nodes[node].Qemu[vmId].Spiceproxy.GetSpiceFileVVAsync(proxy);
        if (!success)
        {
            return reasonPhrase ?? "SPICE proxy request failed";
        }

        return LaunchViewer(OverrideProxy(content, proxy), config, host);
    }

    public static async Task<string> LaunchNodeSpiceAsync(PveClient client, string node, VdiConfig config, VdiHost host)
    {
        var proxy = string.IsNullOrEmpty(host.Spice.Proxy)
            ? client.Host
            : host.Spice.Proxy;

        var (success, reasonPhrase, content) = await client.Nodes[node].Spiceshell.GetSpiceFileVVAsync(proxy);
        if (!success)
        {
            return reasonPhrase ?? "SPICE shell request failed";
        }

        return LaunchViewer(OverrideProxy(content, proxy), config, host);
    }

    private static string OverrideProxy(string content, string proxy)
    {
        if (!ProxyUrlRegex().IsMatch(proxy))
        {
            return content;
        }

        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("proxy="))
            {
                lines[i] = $"proxy={proxy}";
                break;
            }
        }


        return string.Join('\n', lines);
    }

    private static string LaunchViewer(string content, VdiConfig config, VdiHost host)
    {
        var vvFile = System.IO.Path.GetTempFileName().Replace(".tmp", ".vv");
        File.WriteAllText(vvFile, content);

        var viewerPath = config.ViewerPath;
        if (string.IsNullOrWhiteSpace(viewerPath))
        {
            return "SPICE viewer path is not configured. Please set it in Settings → Viewer.";
        }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = $"\"{viewerPath}\"";
            startInfo.Arguments = $"\"{vvFile}\" {viewerOptions}";
        }
        else
        {
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"-c \"{viewerPath} {vvFile} {viewerOptions}\"";
        }

        try { new Process { StartInfo = startInfo }.Start(); return string.Empty; }
        catch (Exception ex) { return ex.Message; }
    }
}
