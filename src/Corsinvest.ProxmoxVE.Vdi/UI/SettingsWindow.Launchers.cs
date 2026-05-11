/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Platform.Storage;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    private static (TabItem Tab, Action Save) BuildTabLaunchers(AppConfig config, Window owner)
    {
        var txtViewerPath = UiHelper.TextBox(config.ViewerPath, L("ViewerPathWatermark"), AppIcons.Console);
        var btnBrowseViewer = UiHelper.IconButton(AppIcons.Folder, "SelectSpiceViewer", margin: new Thickness(4, 0, 0, 0));

        var viewerRow = UiHelper.RowWithButton(txtViewerPath, btnBrowseViewer);

        // Protocol flags
        var chkEnableSpice = new CheckBox
        {
            Content = UiHelper.WithText(AppIcons.Monitor, L("EnableSpice")),
            IsChecked = config.EnableSpice
        };
        var chkEnableVnc = new CheckBox
        {
            Content = UiHelper.WithText(AppIcons.Monitor, L("EnableVnc")),
            IsChecked = config.EnableVnc
        };
        // Launchers list
        var userPath = AppConfigManager.LaunchersUserFile;
        var launchers = LauncherEngine.LoadAll(userPath).ToList();

        string Label(LauncherDefinition def) => $"{def.DisplayName}  [{def.Platform}]";

        Action refresh = null!;

        var toolbarButtons = new List<ToolbarButton>
        {
            new()
            {
                Icon = AppIcons.Add,
                Tooltip = L("Add"),
                OnClick = async () =>
                {
                    var result = await LauncherEditWindow.ShowAsync(owner);
                    if (result is null) { return; }
                    launchers.Add(result);
                    SaveUserOverrides(launchers);
                    refresh();
                }
            },
            new()
            {
                Icon = AppIcons.Refresh,
                Tooltip = L("ResetAllToDefault"),
                OnClick = async () =>
                {
                    if (!await DialogHelper.ConfirmAsync(owner, L("ConfirmResetAllLaunchers"))) { return; }
                    var builtins = LauncherEngine.LoadAll();
                    launchers.Clear();
                    launchers.AddRange(builtins);
                    SaveUserOverrides(launchers);
                    refresh();
                }
            }
        };

        var rowButtons = new List<RowButton<LauncherDefinition>>
        {
            new()
            {
                Icon = AppIcons.Edit,
                Tooltip = L("Edit"),
                IsDoubleClick = true,
                OnClick = async def =>
                {
                    var idx = launchers.IndexOf(def);
                    if (idx < 0) { return; }

                    var result = await LauncherEditWindow.ShowAsync(owner, def);
                    if (result is null) { return; }

                    var builtins = LauncherEngine.LoadAll();
                    var builtin  = builtins.FirstOrDefault(b => b.ServiceId == result.ServiceId);
                    launchers[idx] = builtin is not null ? LauncherEngine.MergeSingle(builtin, result) : result;
                    SaveUserOverrides(launchers);
                    refresh();
                }
            },
            new()
            {
                Icon = AppIcons.Delete,
                Tooltip = L("Delete"),
                Foreground = Brushes.IndianRed,
                IsVisible = def => LauncherEngine.LoadAll().All(b => b.ServiceId != def.ServiceId),
                OnClick = def =>
                {
                    launchers.Remove(def);
                    SaveUserOverrides(launchers);
                    refresh();
                    return Task.CompletedTask;
                }
            },
            new()
            {
                Icon = AppIcons.Book,
                Tooltip = L("Documentation"),
                IsVisible = def => !string.IsNullOrEmpty(def.DocumentationUrl),
                OnClick = def =>
                {
                    if (!string.IsNullOrEmpty(def.DocumentationUrl))
                    {
                        try { Process.Start(new ProcessStartInfo(def.DocumentationUrl) { UseShellExecute = true }); }
                        catch { /* ignore */ }
                    }
                    return Task.CompletedTask;
                }
            }
        };

        var (listPanel, refreshFn) = ActionListBox.Build(launchers,
                                                         Label,
                                                         toolbarButtons,
                                                         rowButtons,
                                                         visibleRows: 6);
        refresh = refreshFn;

        var tab = new TabItem
        {
            Header = UiHelper.WithText(AppIcons.Console, L("TabLaunchers")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    SectionHeader(L("SectionViewer")),
                    UiHelper.Label("ViewerPath"), viewerRow,
                    SectionHeader(L("SectionProtocols")),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 16,
                        Children = { chkEnableSpice, chkEnableVnc }
                    },
                    SectionHeader(L("SectionLaunchers")),
                    listPanel,
                }
            }
        };

        btnBrowseViewer.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(owner);
            if (topLevel == null) { return; }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = L("SelectSpiceViewer"),
                AllowMultiple = false
            });

            if (files.Count > 0) { txtViewerPath.Text = files[0].Path.LocalPath; }
        };

        void Save()
        {
            config.ViewerPath = txtViewerPath.Text?.Trim() ?? string.Empty;
            config.EnableSpice = chkEnableSpice.IsChecked is true;
            config.EnableVnc = chkEnableVnc.IsChecked is true;
        }

        return (tab, Save);
    }

    private static void SaveUserOverrides(IReadOnlyList<LauncherDefinition> current)
    {
        var builtins = LauncherEngine.LoadAll();

        var overrides = current.Where(def =>
        {
            var builtin = builtins.FirstOrDefault(b => b.ServiceId == def.ServiceId);
            if (builtin is null) { return true; }
            return def.Arguments != builtin.Arguments
                    || def.ExtraArgs != builtin.ExtraArgs
                    || def.DisplayName != builtin.DisplayName
                    || def.DefaultPort != builtin.DefaultPort
                    || def.SupportsCredentials != builtin.SupportsCredentials
                    || def.WindowsCredential.Enable != builtin.WindowsCredential.Enable
                    || def.WindowsCredential.Type != builtin.WindowsCredential.Type
                    || def.WindowsCredential.Target != builtin.WindowsCredential.Target
                    || def.Executable != builtin.Executable
                    || def.Platform != builtin.Platform
                    || def.DocumentationUrl != builtin.DocumentationUrl;
        }).ToList();

        LauncherEngine.SaveUserLaunchers(overrides, AppConfigManager.LaunchersUserFile);
    }
}
