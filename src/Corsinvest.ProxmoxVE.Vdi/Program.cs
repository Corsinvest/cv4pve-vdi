/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI;

namespace Corsinvest.ProxmoxVE.Vdi;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var config = AppConfigManager.Load();
        ApplyLanguage(config.Language);

        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnLastWindowClose
        };

        var builder = AppBuilder.Configure<Application>()
                                .UsePlatformDetect()
                                .SetupWithLifetime(lifetime);

        builder.Instance!.Styles.Add(new FluentTheme());
        builder.Instance.RequestedThemeVariant = config.ThemeVariant;

        lifetime.MainWindow = LoginWindow.Create(config);
        lifetime.Start(args);
    }
}
