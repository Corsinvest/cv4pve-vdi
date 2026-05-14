/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private Control BuildHelpMenu(MenuItem menuItemSettings)
    {
        var updateBadge = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = new SolidColorBrush(Colors.OrangeRed),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsVisible = false,
            IsHitTestVisible = false,
            Margin = new Thickness(0, -2, -2, 0)
        };

        var miUpdate = new MenuItem
        {
            Header = UiHelper.WithText(AppIcons.Update, L("UpdateAvailable"), new SolidColorBrush(Colors.OrangeRed)),
            IsVisible = false
        };

        var menuItemSwitchUser = new MenuItem { Header = UiHelper.WithText(AppIcons.Login, L("SwitchUser")) };
        var menuItemDocs = new MenuItem { Header = UiHelper.WithText(AppIcons.Book, L("Documentation")), IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };
        var menuItemRelease = new MenuItem { Header = UiHelper.WithText(AppIcons.Star, L("ReleaseNotes")), IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };
        var menuItemSupport = new MenuItem { Header = UiHelper.WithText(AppIcons.Globe, L("Support")), IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };
        var menuItemBug = new MenuItem { Header = UiHelper.WithText(AppIcons.Bug, L("ReportBug")), IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };
        var menuItemFeature = new MenuItem { Header = UiHelper.WithText(AppIcons.Info, L("RequestFeature")), IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };

        var versionHeader = new TextBlock
        {
            Text = $"cv4pve-vdi  v{ApplicationHelper.Version}",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 4),
            Opacity = 0.7,
            FontSize = 12
        };

        var sepDocs = new Separator { IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };
        var sepBug = new Separator { IsVisible = !_config.Kiosk || KioskGuard.IsAdminUnlocked };

        var menu = new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = versionHeader, IsEnabled = false },
                miUpdate,
                new Separator(),
                menuItemSettings,
                menuItemSwitchUser,
                sepDocs,
                menuItemDocs,
                menuItemRelease,
                menuItemSupport,
                sepBug,
                menuItemBug,
                menuItemFeature,
            }
        };

        menuItemSwitchUser.Click += (_, _) => SwitchUser();
        menuItemDocs.Click += (_, _) => OpenUrl(ApplicationHelper.DocumentationUrl);
        menuItemRelease.Click += (_, _) => OpenUrl(ApplicationHelper.ReleaseNotesUrl);
        menuItemSupport.Click += (_, _) => OpenUrl(ApplicationHelper.SupportUrl);
        menuItemBug.Click += (_, _) => OpenUrl(ApplicationHelper.GetBugReportUrl(_pveVersion));
        menuItemFeature.Click += (_, _) => OpenUrl(ApplicationHelper.FeatureRequestUrl);

        var btn = UiHelper.IconButton(AppIcons.DotsVertical, margin: new Thickness(4, 0, 0, 0));

        var btnGrid = new Grid();
        btnGrid.Children.Add(UiHelper.Icon(AppIcons.DotsVertical));
        btnGrid.Children.Add(updateBadge);
        btn.Content = btnGrid;

        // show badge when update is available
        miUpdate.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == "IsVisible") { updateBadge.IsVisible = miUpdate.IsVisible; }
        };

        ToolTip.SetTip(btn, L("More"));
        btn.Click += (_, _) => menu.Open(btn);

        UpdateChecker.StartBackground((version, url) =>
        {
            miUpdate.Header = UiHelper.WithText(AppIcons.Update, $"{version} {L("UpdateAvailable")}", new SolidColorBrush(Colors.OrangeRed));
            miUpdate.IsVisible = true;
            miUpdate.Click += (_, _) => OpenUrl(url);
        });

        return btn;
    }
}
