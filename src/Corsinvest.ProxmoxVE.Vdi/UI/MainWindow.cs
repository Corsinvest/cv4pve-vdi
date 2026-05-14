/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

/// <summary>State and UI controls for the main window.</summary>
internal partial class MainWindow(PveClient client, ClusterConfig host, AppConfig config, string vdiUser, string vdiPassword)
{

    private readonly PveClient _client = client;
    private readonly ClusterConfig _host = host;
    private readonly AppConfig _config = config;

    private readonly List<ResourceRow> _allRows = [];
    private string _tagColorMap = string.Empty;
    private IReadOnlyDictionary<string, IReadOnlyList<string>> _permissions =
        new Dictionary<string, IReadOnlyList<string>>();

    // cache
    // key=VmId, value=hasSpice — invalidated when VM transitions between running and stopped
    private readonly Dictionary<long, bool> _spiceConfigCache = [];
    // key=VmId, value=osType — read once from Config, never invalidated
    private readonly Dictionary<long, string> _osTypeCache = [];
    // key=VmId, value=SpiceFeatures — read from Config, refreshed when VM stops/starts
    private readonly Dictionary<long, VmFeatures> _featuresCache = [];
    // key=VmId, value=(agentRunning, checkedAt) — re-checked only after AgentPingCacheSeconds
    private readonly Dictionary<long, (bool Running, DateTime CheckedAt)> _agentPingCache = [];
    private const int AgentPingCacheSeconds = 60;
    private const int AgentPingTimeoutMs = 500;

    private string _filterText = string.Empty;
    private readonly HashSet<string> _filterNodes = [];
    private readonly HashSet<string> _filterTags = [];
    private readonly HashSet<string> _filterPools = [];

    // topbar labels ─
    private readonly TextBlock _lblRunning = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12
    };

    private readonly TextBlock _lblStopped = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12
    };

    private readonly TextBlock _lblNodes = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12
    };

    private readonly TextBlock _lblVMs = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12
    };

    private readonly TextBlock _lblCTs = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12
    };

    private readonly ProgressBar _progressBar = new()
    {
        Height = 8,
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        IsVisible = false,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(0)
    };

    private string _pveVersion = string.Empty;

    // refresh state
    private bool _isRefreshing = false;
    private bool _kioskUnlocked = false;

    private void SwitchUser()
    {
        KioskGuard.ResetAdmin();
        _kioskUnlocked = true;
        var login = LoginWindow.Create(_config);
        login.Show();
        _window?.Close();
    }

    private readonly StackPanel _cardContent = new() { Spacing = 24 };
    private readonly StackPanel _listContent = new() { Spacing = 16, IsVisible = false };

    private ScrollViewer? _sidebar;

    private readonly TextBox _txtSearch = new() { PlaceholderText = L("SearchWatermark") };
    private readonly StackPanel _nodeFilters = new() { Spacing = 4 };
    private readonly StackPanel _poolFilters = new() { Spacing = 4 };
    private readonly StackPanel _tagFilters = new() { Spacing = 4 };
    private Border? _sectionNodes;
    private Border? _sectionPools;
    private Border? _sectionTags;

    private readonly CheckBox _chkRunning = new()
    {
        Content = MakeDotLabel(AppColors.Running, L("StatusRunning")),
        IsChecked = true
    };
    private readonly CheckBox _chkStopped = new()
    {
        Content = MakeDotLabel(AppColors.Stopped, L("StatusStopped")),
        IsChecked = false
    };
    private readonly CheckBox _chkQemu = new()
    {
        Content = UiHelper.WithText(AppIcons.Vm, L("TypeVm")),
        IsChecked = false
    };
    private readonly CheckBox _chkLxc = new()
    {
        Content = UiHelper.WithText(AppIcons.Ct, L("TypeCt")),
        IsChecked = false
    };
    private readonly Button _btnReset = new()
    {
        Content = UiHelper.Icon(AppIcons.Close, 12),
        Padding = new Thickness(4),
        Background = Brushes.Transparent,
        BorderBrush = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Right,
        Opacity = 0.55
    };

    private Window? _window;
    private Button? _btnRefresh;
    private ToggleButton? _btnAutoRef;

    internal static WindowIcon AppIcon()
    {
        using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://cv4pve-vdi/Corsinvest.ico"));
        return new WindowIcon(stream);
    }

    private static StackPanel MakeDotLabel(Color c, string label) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        Children =
        {
            new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(c),
                VerticalAlignment = VerticalAlignment.Center
            },
            new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            }
        }
    };

    public Window Build()
    {
        _btnRefresh = UiHelper.IconButton(AppIcons.Refresh);
        var btnRefresh = _btnRefresh;

        var autoRefLabel = new TextBlock
        {
            Text = "30s",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            IsVisible = false
        };
        _btnAutoRef = new ToggleButton
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { UiHelper.Icon(AppIcons.AutoRefresh), autoRefLabel }
            },
            Padding = new Thickness(6, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        var btnAutoRef = _btnAutoRef;
        btnAutoRef.IsCheckedChanged += (_, _) => autoRefLabel.IsVisible = btnAutoRef.IsChecked is true;

        var menuItemSettings = new MenuItem { Header = UiHelper.WithText(AppIcons.Settings, L("Settings")) };
        var btnMore = BuildHelpMenu(menuItemSettings);

        ToolTip.SetTip(btnRefresh, L("Refresh"));
        ToolTip.SetTip(btnAutoRef, L("AutoRefresh"));

        var statsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var chipRunning = StatChipDot(_lblRunning, AppColors.Running, L("StatusRunning"));
        var chipStopped = StatChipDot(_lblStopped, AppColors.Stopped, L("StatusStopped"));
        var chipNodes = StatChipIcon(AppIcons.Server, _lblNodes, AppColors.StatNodes, L("TypeNode"));
        var chipVMs = StatChipIcon(AppIcons.Vm, _lblVMs, AppColors.StatVMs, L("TypeVm"));
        var chipCTs = StatChipIcon(AppIcons.Ct, _lblCTs, AppColors.StatCTs, L("TypeCt"));
        statsPanel.Children.Add(chipRunning);
        statsPanel.Children.Add(chipStopped);
        statsPanel.Children.Add(chipNodes);
        statsPanel.Children.Add(chipVMs);
        statsPanel.Children.Add(chipCTs);

        void RefreshChipColors()
        {
            chipRunning.Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, AppColors.Running.R, AppColors.Running.G, AppColors.Running.B));
            chipStopped.Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, AppColors.Stopped.R, AppColors.Stopped.G, AppColors.Stopped.B));
            chipNodes.Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, AppColors.StatNodes.R, AppColors.StatNodes.G, AppColors.StatNodes.B));
            chipVMs.Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, AppColors.StatVMs.R, AppColors.StatVMs.G, AppColors.StatVMs.B));
            chipCTs.Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, AppColors.StatCTs.R, AppColors.StatCTs.G, AppColors.StatCTs.B));
        }

        var logoLbl = new TextBlock
        {
            Text = _host.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnPanel.Children.Add(btnRefresh);
        btnPanel.Children.Add(btnAutoRef);
        btnPanel.Children.Add(btnMore);

        var topbarInner = new AGrid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        topbarInner.Add(logoLbl, 0);
        topbarInner.Add(statsPanel, 1);
        topbarInner.Add(btnPanel, 2);

        var topbar = new Border
        {
            Padding = new Thickness(14, 8),
            Child = topbarInner
        };

        _sidebar = new ScrollViewer
        {
            Width = 210,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new StackPanel
            {
                Margin = new Thickness(12, 14),
                Spacing = 0,
                Children =
                {
                    BuildFiltersHeader(),
                    SideSection(L("SectionSearch"), _txtSearch),
                    SideSection(L("SectionStatus"), new StackPanel
                    {
                        Spacing = 4,
                        Children = { _chkRunning, _chkStopped }
                    }),
                    SideSection(L("SectionType"), new StackPanel
                    {
                        Spacing = 4,
                        Children = { _chkQemu, _chkLxc }
                    }),
                    (_sectionNodes = SideSection(L("SectionNodes"), new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _nodeFilters
                    }, AppIcons.Server)),
                    (_sectionPools = SideSection(L("SectionPools"), new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _poolFilters
                    }, AppIcons.Folder)),
                    (_sectionTags = SideSection(L("SectionTags"), new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _tagFilters
                    }, AppIcons.Tag)),
                }
            }
        };

        var scrollContent = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(18, 14),
                Spacing = 0,
                Children = { _cardContent, _listContent }
            },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var btnConfigureViewer = new Button
        {
            Content = L("OpenSettings"),
            Padding = new Thickness(8, 2),
            Margin = new Thickness(12, 0, 0, 0)
        };

        var viewerWarningBanner = BuildPersistentBanner(btnConfigureViewer);

        btnConfigureViewer.Click += async (_, _) =>
        {
            var w = SettingsWindow.Create(_config, initialTab: 1);
            w.Icon = AppIcon();
            await w.ShowDialog(_window!);

            if (w.Tag as string == "reopen")
            {
                var w2 = SettingsWindow.Create(_config, initialTab: 1);
                w2.Icon = AppIcon();
                await w2.ShowDialog(_window!);
            }

            UpdateViewerWarning();
        };

        var mainGrid = new AGrid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        AGrid.SetRow(topbar, 0);
        AGrid.SetColumnSpan(topbar, 2);
        mainGrid.Children.Add(topbar);
        AGrid.SetRow(viewerWarningBanner, 1);
        AGrid.SetColumnSpan(viewerWarningBanner, 2);
        mainGrid.Children.Add(viewerWarningBanner);
        mainGrid.Add(scrollContent, 0, 2);
        mainGrid.Add(_sidebar, 1, 2);
        AGrid.SetRow(_progressBar, 3);
        AGrid.SetColumnSpan(_progressBar, 2);
        mainGrid.Children.Add(_progressBar);

        // toast overlay — spans full grid, pointer passthrough except on toasts
        AGrid.SetRow(_toastStack, 2);
        AGrid.SetColumnSpan(_toastStack, 2);
        mainGrid.Children.Add(_toastStack);

        _window = new Window
        {
            Title = "cv4pve-vdi — Proxmox VDI by Corsinvest",
            Width = 1200,
            Height = 700,
            MinWidth = 800,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = mainGrid
        };

        if (_config.Kiosk)
        {
            if (_config.KioskForceFullScreen) { _window.WindowState = WindowState.FullScreen; }
            _window.Closing += async (_, e) =>
            {
                if (_kioskUnlocked) { return; }
                e.Cancel = true;
                if (await KioskGuard.CheckAsync(_window!, _config))
                {
                    _kioskUnlocked = true;
                    _window.Close();
                }
            };
        }

        void ApplyDefaultView()
        {
            var isList = _config.DefaultView == AppConfig.ViewList;
            _cardContent.IsVisible = !isList;
            _listContent.IsVisible = isList;
            scrollContent.HorizontalScrollBarVisibility = isList ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        }

        _txtSearch.TextChanged += (_, _) =>
        {
            _filterText = _txtSearch.Text ?? string.Empty;
            ApplyFilter();
        };
        _chkRunning.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkStopped.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkQemu.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkLxc.IsCheckedChanged += (_, _) => ApplyFilter();

        _btnReset.Click += (_, _) =>
        {
            _txtSearch.Text = string.Empty;
            _chkRunning.IsChecked = _chkStopped.IsChecked = false;
            _chkQemu.IsChecked = _chkLxc.IsChecked = false;
            _filterNodes.Clear();
            foreach (var child in _nodeFilters.Children.OfType<CheckBox>())
            {
                child.IsChecked = false;
            }

            _filterPools.Clear();
            foreach (var child in _poolFilters.Children.OfType<CheckBox>())
            {
                child.IsChecked = false;
            }

            _filterTags.Clear();
            foreach (var child in _tagFilters.Children.OfType<CheckBox>())
            {
                child.IsChecked = false;
            }

            ApplyFilter();
        };

        menuItemSettings.Click += async (_, _) =>
        {
            var prevEnableSpice = _config.EnableSpice;
            var prevEnableVnc = _config.EnableVnc;
            var prevEnableAgentPing = _config.EnableAgentPing;
            var prevShowNodes = _config.ShowNodes;
            var prevShowPools = _config.ShowPools;
            var prevShowTags = _config.ShowTags;
            var prevViewerPath = _config.ViewerPath;

            var w = SettingsWindow.Create(_config);
            w.Icon = AppIcon();
            await w.ShowDialog(_window!);

            // Admin just unlocked — reopen Settings so the advanced tabs become visible
            if (w.Tag as string == "reopen")
            {
                var w2 = SettingsWindow.Create(_config);
                w2.Icon = AppIcon();
                await w2.ShowDialog(_window!);
            }

            ApplySidebarVisibility();
            ApplyDefaultView();
            UpdateViewerWarning();

            // Protocol flags changed → clear relevant caches
            if (_config.EnableSpice != prevEnableSpice || _config.ViewerPath != prevViewerPath)
            {
                _spiceConfigCache.Clear();
                _featuresCache.Clear();
            }
            if (_config.EnableAgentPing != prevEnableAgentPing)
            {
                _agentPingCache.Clear();
            }

            // Newly enabled sidebar sections need data → refresh
            var needsRefresh = (_config.EnableSpice != prevEnableSpice)
                            || (_config.EnableVnc != prevEnableVnc)
                            || (_config.EnableAgentPing != prevEnableAgentPing)
                            || (_config.ShowNodes && !prevShowNodes)
                            || (_config.ShowPools && !prevShowPools)
                            || (_config.ShowTags && !prevShowTags)
                            || (_config.ViewerPath != prevViewerPath);


            if (needsRefresh)
            {
                await RefreshAsync();
            }
            else
            {
                ApplyFilter();
            }
        };


        btnRefresh.Click += async (_, _) => await RefreshAsync();

        var autoRefTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        autoRefTimer.Tick += async (_, _) =>
        {
            if (_isRefreshing)
            {
                return;
            }

            await RefreshAsync();
        };

        btnAutoRef.IsCheckedChanged += (_, _) =>
        {
            if (btnAutoRef.IsChecked is true)
            {
                autoRefTimer.Start();
            }
            else
            {
                autoRefTimer.Stop();
            }
        };

        _window.Closing += (_, _) => autoRefTimer.Stop();
        _window.Opened += async (_, _) =>
        {
            ApplySidebarVisibility();
            ApplyDefaultView();
            UpdateViewerWarning();
            await RefreshAsync();
        };

        Application.Current?.ActualThemeVariantChanged += (_, _) =>
        {
            RefreshChipColors();
            UpdateViewerWarning();
            ApplyFilter();
        };

        return _window;
    }

    private static byte ChipAlpha => AppColors.IsDark ? (byte)40 : (byte)20;

    private static Border StatChipDot(TextBlock valueLabel, Color dotColor, string label)
        => new()
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, dotColor.R, dotColor.G, dotColor.B)),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(dotColor),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    valueLabel,
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 11,
                        Opacity = 0.55,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

    private static Border StatChipIcon(string iconData, TextBlock valueLabel, Color iconColor, string label)
        => new()
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(ChipAlpha, iconColor.R, iconColor.G, iconColor.B)),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    UiHelper.Icon(iconData, 13, new SolidColorBrush(iconColor)),
                    valueLabel,
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 11,
                        Opacity = 0.55,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

    internal void UpdateViewerWarning()
    {
        if (string.IsNullOrEmpty(_config.ViewerPath))
        {
            ShowBanner(L("ViewerNotConfigured"), NotificationSeverity.Warning);
        }
        else
        {
            HideBanner();
        }
    }

    internal void ApplySidebarVisibility()
    {
        _sectionNodes?.IsVisible = _config.ShowNodes;
        _sectionPools?.IsVisible = _config.ShowPools;
        _sectionTags?.IsVisible = _config.ShowTags;
    }

    private Border BuildFiltersHeader()
    {
        var headerGrid = new AGrid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        var lbl = new TextBlock
        {
            Text = L("SectionFilters"),
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        AGrid.SetColumn(lbl, 0);
        AGrid.SetColumn(_btnReset, 1);
        headerGrid.Children.Add(lbl);
        headerGrid.Children.Add(_btnReset);
        ToolTip.SetTip(_btnReset, L("ResetFilters"));
        return new Border
        {
            Padding = new Thickness(0, 0, 0, 10),
            Child = headerGrid
        };
    }

    private static Border SideSection(string title, Control content, string? icon = null)
        => new()
        {
            Padding = new Thickness(0, 0, 0, 14),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    icon == null
                        ? new TextBlock
                        {
                            Text = title,
                            FontSize = 10,
                            FontWeight = FontWeight.Bold,
                            Opacity = 0.5,
                            Margin = new Thickness(0, 0, 0, 2)
                        }
                        : new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 4,
                            Margin = new Thickness(0, 0, 0, 2),
                            Children =
                            {
                                new PathIcon
                                {
                                    Data = Geometry.Parse(icon),
                                    Width = 10,
                                    Height = 10,
                                    Opacity = 0.5,
                                    VerticalAlignment = VerticalAlignment.Center
                                },
                                new TextBlock
                                {
                                    Text = title,
                                    FontSize = 10,
                                    FontWeight = FontWeight.Bold,
                                    Opacity = 0.5,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            }
                        },
                    content
                }
            }
        };
}
