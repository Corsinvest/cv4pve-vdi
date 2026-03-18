/*
using Corsinvest.ProxmoxVE.Vdi.UI;
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI;

var config = VdiConfigManager.Load();

Window Build() => LoginWindow.Create(config);

AppBuilder.Configure<Application>()
          .UsePlatformDetect()
          .WithApplicationName("cv4pve-vdi")
          .UseFluentTheme()
          .AfterSetup(b =>
          {
              if (b.Instance == null) { return; }

              b.Instance.RequestedThemeVariant = config.Theme switch
              {
                  "Light" => ThemeVariant.Light,
                  "Dark" => ThemeVariant.Dark,
                  _ => ThemeVariant.Default
              };
          })
          .StartWithClassicDesktopLifetime(Build, args);
