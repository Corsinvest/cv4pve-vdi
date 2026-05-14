/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    private static (TabItem Tab, Action Save) BuildTabAppearance(AppConfig config)
    {
        var themes = new[] { AppConfig.ThemeSystem, AppConfig.ThemeLight, AppConfig.ThemeDark };

        var cmbTheme = new ComboBox
        {
            ItemsSource = themes,
            SelectedItem = themes.Contains(config.Theme) ? config.Theme : AppConfig.ThemeSystem,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 120,
            Padding = new Thickness(26, 0, 0, 0)
        };
        var cmbThemeWithIcon = UiHelper.WithIcon(cmbTheme, AppIcons.Palette);

        var isCard = config.DefaultView != AppConfig.ViewList;
        var btnViewCard = new ToggleButton
        {
            Content = UiHelper.WithText(AppIcons.ViewGrid, AppConfig.ViewCard),
            IsChecked = isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        var btnViewList = new ToggleButton
        {
            Content = UiHelper.WithText(AppIcons.ViewDetail, AppConfig.ViewList),
            IsChecked = !isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        btnViewCard.IsCheckedChanged += (_, _) => { if (btnViewCard.IsChecked is true) { btnViewList.IsChecked = false; } };
        btnViewList.IsCheckedChanged += (_, _) => { if (btnViewList.IsChecked is true) { btnViewCard.IsChecked = false; } };

        var viewToggle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        viewToggle.Children.Add(btnViewCard);
        viewToggle.Children.Add(btnViewList);

        var themeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = L("Theme"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
                cmbThemeWithIcon,
                new TextBlock { Text = L("DefaultView"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) },
                viewToggle
            }
        };

        var chkShowBars = new CheckBox { Content = UiHelper.WithText(AppIcons.ChartBar, L("ShowBars")), IsChecked = config.ShowBars };
        var chkShowNodes = new CheckBox { Content = UiHelper.WithText(AppIcons.Server, L("ShowNodes")), IsChecked = config.ShowNodes };
        var chkShowPools = new CheckBox { Content = UiHelper.WithText(AppIcons.Folder, L("ShowPools")), IsChecked = config.ShowPools };
        var chkShowTags = new CheckBox { Content = UiHelper.WithText(AppIcons.Tag, L("ShowTags")), IsChecked = config.ShowTags };

        var chkEnableAgentPing = new CheckBox { Content = UiHelper.WithText(AppIcons.Server, L("EnableAgentPing")), IsChecked = config.EnableAgentPing };

        var chkShowStart = new CheckBox { Content = UiHelper.WithText(AppIcons.Play, L("ShowStartButton"), new SolidColorBrush(AppColors.Running)), IsChecked = config.ShowStartButton };
        var chkConfirmStart = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmStart, IsEnabled = config.ShowStartButton, HorizontalAlignment = HorizontalAlignment.Right };
        var chkShowShutdown = new CheckBox { Content = UiHelper.WithText(AppIcons.Stop, L("ShowShutdownButton"), new SolidColorBrush(AppColors.Shutdown)), IsChecked = config.ShowShutdownButton };
        var chkConfirmShutdown = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmShutdown, IsEnabled = config.ShowShutdownButton, HorizontalAlignment = HorizontalAlignment.Right };

        Grid.SetColumn(chkConfirmStart, 1);
        Grid.SetColumn(chkConfirmShutdown, 1);

        chkShowStart.IsCheckedChanged += (_, _) => chkConfirmStart.IsEnabled = chkShowStart.IsChecked is true;
        chkShowShutdown.IsCheckedChanged += (_, _) => chkConfirmShutdown.IsEnabled = chkShowShutdown.IsChecked is true;

        var advancedSection = new StackPanel
        {
            Spacing = 8,
            IsVisible = !config.Kiosk || KioskGuard.IsAdminUnlocked,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    RowDefinitions    = new RowDefinitions("Auto,Auto"),
                    Children =
                    {
                        chkShowBars,
                        Placed(chkShowNodes, 1, 0),
                        Placed(chkShowPools, 0, 1),
                        Placed(chkShowTags,  1, 1),
                    }
                },
                SectionHeader(L("SectionBehaviour")),
                chkEnableAgentPing,
                SectionHeader(L("PowerButtons")),
                new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { chkShowStart,    chkConfirmStart } },
                new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { chkShowShutdown, chkConfirmShutdown } },
            }
        };

        var tab = new TabItem
        {
            Header = UiHelper.WithText(AppIcons.Palette, L("TabAppearance")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    SectionHeader(L("SectionDisplay")),
                    themeRow,
                    advancedSection,
                }
            }
        };

        void Save()
        {
            config.Theme = cmbTheme.SelectedItem as string ?? AppConfig.ThemeSystem;
            config.DefaultView = btnViewList.IsChecked is true ? AppConfig.ViewList : AppConfig.ViewCard;
            config.ShowBars = chkShowBars.IsChecked is true;
            config.ShowNodes = chkShowNodes.IsChecked is true;
            config.ShowPools = chkShowPools.IsChecked is true;
            config.ShowTags = chkShowTags.IsChecked is true;
            config.ShowStartButton = chkShowStart.IsChecked is true;
            config.ConfirmStart = chkConfirmStart.IsChecked is true;
            config.ShowShutdownButton = chkShowShutdown.IsChecked is true;
            config.ConfirmShutdown = chkConfirmShutdown.IsChecked is true;
            config.EnableAgentPing = chkEnableAgentPing.IsChecked is true;
        }

        return (tab, Save);
    }

    private static Control Placed(Control c, int col, int row)
    {
        Grid.SetColumn(c, col);
        Grid.SetRow(c, row);
        return c;
    }
}
