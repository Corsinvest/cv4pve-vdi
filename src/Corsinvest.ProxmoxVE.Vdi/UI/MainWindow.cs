/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

/// <summary>State and UI controls for the main window.</summary>
internal partial class MainWindow(PveClient client, VdiHost host, VdiConfig config)
{
    private readonly PveClient _client = client;
    private readonly VdiHost _host = host;
    private readonly VdiConfig _config = config;

    private readonly List<ResourceRow> _allRows = [];
    private string _tagColorMap = string.Empty;
    private IReadOnlyDictionary<string, IReadOnlyList<string>> _permissions =
        new Dictionary<string, IReadOnlyList<string>>();

    // cache
    // key=VmId, value=hasSpice — invalidated when VM transitions between running and stopped
    private readonly Dictionary<long, bool> _spiceConfigCache = [];
    // key=VmId, value=(hasRdp, ip) — cleared on each refresh when VM is not running
    private readonly Dictionary<long, (bool HasRdp, string? Ip)> _rdpCache = [];
    // key=VmId, value=osType — read once from Config, never invalidated
    private readonly Dictionary<long, string> _osTypeCache = [];

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

    private readonly TextBlock _lblStatus = new()
    {
        Text = L("Loading"),
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 11,
        Margin = new Thickness(4, 0, 0, 0)
    };

    private readonly ProgressBar _progressBar = new()
    {
        Height = 5,
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        IsVisible = false,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(0)
    };

    // refresh state
    private bool _isRefreshing = false;

    private readonly StackPanel _cardContent = new() { Spacing = 24 };
    private readonly StackPanel _listContent = new() { Spacing = 16, IsVisible = false };

    private ScrollViewer? _sidebar;

    private readonly TextBox _txtSearch = new() { Watermark = L("SearchWatermark") };
    private readonly StackPanel _nodeFilters = new() { Spacing = 4 };
    private readonly StackPanel _poolFilters = new() { Spacing = 4 };
    private readonly StackPanel _tagFilters = new() { Spacing = 4 };

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
        Content = AppIcons.WithText(AppIcons.Vm, L("TypeVm")),
        IsChecked = false
    };
    private readonly CheckBox _chkLxc = new()
    {
        Content = AppIcons.WithText(AppIcons.Ct, L("TypeCt")),
        IsChecked = false
    };
    private readonly Button _btnReset = new()
    {
        Content = AppIcons.WithText(AppIcons.Close, L("ResetFilters")),
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private Window? _window;
    private ToggleButton? _btnAutoRef;
    private readonly Button _btnUpdate = new()
    {
        IsVisible = false,
        Padding = new Thickness(8, 4),
        Margin = new Thickness(8, 0, 0, 0),
        Background = new SolidColorBrush(Color.Parse("#1976D2")),
        Foreground = Brushes.White,
        CornerRadius = new CornerRadius(4),
    };

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
        var btnCardView = new ToggleButton
        {
            Content = AppIcons.Toolbar(AppIcons.ViewGrid),
            Padding = new Thickness(6, 4),
            IsChecked = true
        };
        var btnListView = new ToggleButton
        {
            Content = AppIcons.Toolbar(AppIcons.ViewDetail),
            Padding = new Thickness(6, 4)
        };
        var btnRefresh = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Refresh),
            Padding = new Thickness(6, 4)
        };
        _btnAutoRef = new ToggleButton
        {
            Content = AppIcons.Toolbar(AppIcons.AutoRefresh),
            Padding = new Thickness(6, 4)
        };
        var btnAutoRef = _btnAutoRef;
        var btnSettings = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Settings),
            Padding = new Thickness(6, 4)
        };
        var btnAbout = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Info),
            Padding = new Thickness(6, 4)
        };

        _lblStatus.Secondary();

        Avalonia.Controls.ToolTip.SetTip(btnCardView, L("CardView"));
        Avalonia.Controls.ToolTip.SetTip(btnListView, L("DetailView"));
        Avalonia.Controls.ToolTip.SetTip(btnRefresh, L("Refresh"));
        Avalonia.Controls.ToolTip.SetTip(btnAutoRef, L("AutoRefresh"));
        Avalonia.Controls.ToolTip.SetTip(btnSettings, L("Settings"));
        Avalonia.Controls.ToolTip.SetTip(btnAbout, L("About"));

        var statsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        statsPanel.Children.Add(StatChipDot(_lblRunning, AppColors.Running, L("StatusRunning")));
        statsPanel.Children.Add(StatChipDot(_lblStopped, AppColors.Stopped, L("StatusStopped")));
        statsPanel.Children.Add(StatChipIcon(AppIcons.Server, _lblNodes, AppColors.StatNodes, L("TypeNode")));
        statsPanel.Children.Add(StatChipIcon(AppIcons.Vm, _lblVMs, AppColors.StatVMs, L("TypeVm")));
        statsPanel.Children.Add(StatChipIcon(AppIcons.Ct, _lblCTs, AppColors.StatCTs, L("TypeCt")));

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
        btnPanel.Children.Add(_lblStatus);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnCardView);
        btnPanel.Children.Add(btnListView);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnRefresh);
        btnPanel.Children.Add(btnAutoRef);
        btnPanel.Children.Add(btnSettings);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnAbout);
        btnPanel.Children.Add(_btnUpdate);

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
                    _btnReset,
                    SideSection(L("SectionSearch"), _txtSearch),
                    SideSection(L("SectionNodes"),  new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _nodeFilters
                    }),
                    SideSection(L("SectionPools"),  new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _poolFilters
                    }),
                    SideSection(L("SectionStatus"), new StackPanel
                    {
                        Spacing = 4,
                        Children = { _chkRunning, _chkStopped }
                    }),
                    SideSection(L("SectionType"),   new StackPanel
                    {
                        Spacing = 4,
                        Children = { _chkQemu, _chkLxc }
                    }),
                    SideSection(L("SectionTags"),   new ScrollViewer
                    {
                        MaxHeight = 150,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = _tagFilters
                    }),
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

        var mainGrid = new AGrid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        AGrid.SetRow(topbar, 0);
        AGrid.SetColumnSpan(topbar, 2);
        mainGrid.Children.Add(topbar);
        mainGrid.Add(scrollContent, 0, 1);
        mainGrid.Add(_sidebar, 1, 1);
        AGrid.SetRow(_progressBar, 2);
        AGrid.SetColumnSpan(_progressBar, 2);
        mainGrid.Children.Add(_progressBar);

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

        btnCardView.IsCheckedChanged += (_, _) =>
        {
            if (btnCardView.IsChecked == true)
            {
                btnListView.IsChecked = false;
                _cardContent.IsVisible = true;
                _listContent.IsVisible = false;
                scrollContent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        };
        btnListView.IsCheckedChanged += (_, _) =>
        {
            if (btnListView.IsChecked == true)
            {
                btnCardView.IsChecked = false;
                _cardContent.IsVisible = false;
                _listContent.IsVisible = true;
                scrollContent.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        };

        _txtSearch.TextChanged += (_, _) =>
        {
            _filterText = _txtSearch.Text ?? "";
            ApplyFilter();
        };
        _chkRunning.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkStopped.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkQemu.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkLxc.IsCheckedChanged += (_, _) => ApplyFilter();

        _btnReset.Click += (_, _) =>
        {
            _txtSearch.Text = "";
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

        btnSettings.Click += async (_, _) =>
        {
            var w = SettingsWindow.Create(_config);
            w.Icon = AppIcon();
            await w.ShowDialog(_window);
        };

        btnAbout.Click += async (_, _) => await ShowAboutAsync();

        btnRefresh.Click += async (_, _) => await RefreshAsync(btnRefresh, btnAutoRef);

        CancellationTokenSource? autoRefreshCts = null;
        btnAutoRef.IsCheckedChanged += (_, _) =>
        {
            if (btnAutoRef.IsChecked == true)
            {
                autoRefreshCts = new CancellationTokenSource();
                var token = autoRefreshCts.Token;
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(30_000, token).ContinueWith(_ => { });
                        if (!token.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => RefreshAsync(btnRefresh, btnAutoRef));
                        }
                    }
                }, token);
            }
            else
            {
                autoRefreshCts?.Cancel();
                autoRefreshCts = null;
            }
        };

        _window.Closing += (_, _) => autoRefreshCts?.Cancel();
        _window.Opened += async (_, _) =>
        {
            await RefreshAsync(btnRefresh, btnAutoRef);

            UpdateChecker.StartBackground((version, url) =>
            {
                _btnUpdate.Content = AppIcons.WithText(AppIcons.Update, $"v{version} available");
                _btnUpdate.IsVisible = true;
                Avalonia.Controls.ToolTip.SetTip(_btnUpdate, $"New version {version} available — click to open release page");
                _btnUpdate.Click += (_, _) => TopLevel.GetTopLevel(_window)?.Launcher.LaunchUriAsync(new Uri(url));
            });
        };

        Avalonia.Application.Current?.ActualThemeVariantChanged += (_, _) =>
            {
                _lblStatus.Secondary();
                ApplyFilter();
            };

        return _window;
    }

    private static Control StatChipDot(TextBlock valueLabel, Color dotColor, string label)
        => new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(20, dotColor.R, dotColor.G, dotColor.B)),
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

    private static Control StatChipIcon(string iconData, TextBlock valueLabel, Color iconColor, string label)
        => new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(20, iconColor.R, iconColor.G, iconColor.B)),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                         Data = Geometry.Parse(iconData),
                         Width = 13,
                         Height = 13,
                         Foreground = new SolidColorBrush(iconColor)
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

    private static Border SideSection(string title, Control content)
        => new()
        {
            Padding = new Thickness(0, 0, 0, 14),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 10,
                        FontWeight = FontWeight.Bold,
                        Opacity = 0.5,
                        Margin = new Thickness(0, 0, 0, 2)
                    },
                    content
                }
            }
        };
}
