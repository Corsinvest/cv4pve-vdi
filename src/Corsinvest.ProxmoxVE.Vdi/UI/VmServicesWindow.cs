/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using System.Collections.ObjectModel;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class VmServicesWindow
{
    /// <summary>
    /// Opens the service list window for the given VM.
    /// Returns the updated VmConfig (or the original if cancelled).
    /// </summary>
    public static async Task<VmConfig> ShowAsync(Window owner, VmConfig vmConfig, string vmName, string? userLaunchersPath,
                                                  PveClient? client = null, string? node = null)
    {
        var launchers = LauncherEngine.LoadForCurrentPlatform(userLaunchersPath);
        var services = new ObservableCollection<VmServiceConfig>(vmConfig.Services.Select(s => Clone(s)));

        string Label(VmServiceConfig svc)
        {
            var launcher = launchers.FirstOrDefault(l => l.ServiceId == svc.ServiceId);
            var name = launcher?.DisplayName ?? svc.ServiceId;
            return $"{name}  —  port {svc.Port}  [{svc.CredentialSource}]";
        }

        Action refresh = null!;

        var progressBar = new ProgressBar
        {
            IsIndeterminate = true,
            IsVisible = false,
            Height = 4,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var toolbarButtons = new List<ToolbarButton>
        {
            new()
            {
                Icon    = AppIcons.Add,
                Tooltip = L("Add"),
                OnClick = async () =>
                {
                    var result = await VmServiceEditWindow.ShowAsync(owner, null, launchers);
                    if (result is null) { return; }
                    services.Add(result);
                    refresh();
                }
            },
            new()
            {
                Icon    = AppIcons.Search,
                Tooltip = L("Discover"),
                OnClick = async () =>
                {
                    if (client is null || node is null) { return; }

                    progressBar.IsVisible = true;
                    try
                    {
                        var ip = await VmService.GetVmIpAsync(client, node, vmConfig.VmId);
                        if (string.IsNullOrEmpty(ip))
                        {
                            await DialogHelper.MessageAsync(owner, L("NoIpAvailable"), NotificationSeverity.Error);
                            return;
                        }

                        var found = await VmService.DiscoverServicesAsync(ip, launchers, services);
                        if (found.Count == 0)
                        {
                            await DialogHelper.MessageAsync(owner, L("DiscoverNoneFound"));
                            return;
                        }

                        var selected = await ShowDiscoverDialogAsync(owner, found);
                        foreach (var l in selected)
                        {
                            services.Add(new VmServiceConfig { ServiceId = l.ServiceId, Port = l.DefaultPort });
                        }
                        if (selected.Count > 0) { refresh(); }
                    }
                    finally
                    {
                        progressBar.IsVisible = false;
                    }
                }
            }
        };

        var rowButtons = new List<RowButton<VmServiceConfig>>
        {
            new()
            {
                Icon          = AppIcons.Edit,
                Tooltip       = L("Edit"),
                IsDoubleClick = true,
                OnClick       = async svc =>
                {
                    var idx = services.IndexOf(svc);
                    if (idx < 0) { return; }
                    var result = await VmServiceEditWindow.ShowAsync(owner, svc, launchers);
                    if (result is null) { return; }
                    services[idx] = result;
                    refresh();
                }
            },
            new()
            {
                Icon       = AppIcons.Delete,
                Tooltip    = L("Delete"),
                Foreground = Brushes.IndianRed,
                OnClick    = svc =>
                {
                    services.Remove(svc);
                    refresh();
                    return Task.CompletedTask;
                }
            }
        };

        var (listPanel, refreshFn) = ActionListBox.Build(
            services,
            Label,
            toolbarButtons,
            rowButtons,
            visibleRows: 6);

        refresh = refreshFn;

        var btnSave = UiHelper.IconButton(AppIcons.Save, "Save");

        var window = new Window
        {
            Title = $"{vmName} — {L("Services")}",
            Width = 480,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    progressBar,
                    listPanel,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { btnSave }
                    }
                }
            }
        };

        btnSave.Click += (_, _) => window.Close(new VmConfig { VmId = vmConfig.VmId, Services = [.. services] });

        var updated = await window.ShowDialog<VmConfig?>(owner);
        return updated ?? vmConfig;
    }

    private static VmServiceConfig Clone(VmServiceConfig s) => new()
    {
        ServiceId = s.ServiceId,
        Port = s.Port,
        IpOverride = s.IpOverride,
        ExtraArgs = s.ExtraArgs,
        CredentialSource = s.CredentialSource,
        Credentials = s.Credentials is null
                                ? null
                                : new Credentials
                                {
                                    Username = s.Credentials.Username,
                                    Password = s.Credentials.Password
                                }
    };

    private static async Task<IReadOnlyList<LauncherDefinition>> ShowDiscoverDialogAsync(
        Window owner, IReadOnlyList<LauncherDefinition> found)
    {
        var checkboxes = found.Select(l => new CheckBox
        {
            Content = $"{l.DisplayName}  (port {l.DefaultPort})",
            IsChecked = true
        }).ToList();

        var btnAdd = new Button
        {
            Content = L("AddSelected"),
            Padding = new Thickness(12, 4),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var btnCancel = new Button
        {
            Content = L("Cancel"),
            Padding = new Thickness(12, 4),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var tcs = new TaskCompletionSource<bool>();

        btnAdd.Click += (_, _) => tcs.TrySetResult(true);
        btnCancel.Click += (_, _) => tcs.TrySetResult(false);

        var itemsPanel = new StackPanel { Spacing = 6 };
        foreach (var chk in checkboxes) { itemsPanel.Children.Add(chk); }

        var dlg = new Window
        {
            Title = L("Discover"),
            Width = 320,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = L("DiscoverFound"), FontWeight = FontWeight.SemiBold },
                    itemsPanel,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { btnAdd, btnCancel }
                    }
                }
            }
        };

        dlg.Closed += (_, _) => tcs.TrySetResult(false);

        await dlg.ShowDialog(owner);
        var confirmed = await tcs.Task;

        if (!confirmed) { return []; }

        return [.. found.Where((_, i) => checkboxes[i].IsChecked is true)];
    }
}
