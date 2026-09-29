# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Simplified and Traditional Chinese** UI translations, in the language picker of the login window (#42)

### Documentation
- **Documentation site.** The documentation moved from Markdown files in the repository to [corsinvest.github.io/cv4pve-vdi](https://corsinvest.github.io/cv4pve-vdi/): getting started, permissions, how it connects, the main window, SPICE and VNC, services, launchers, guest setup, kiosk mode, settings and languages. Every page was checked against the code
- New product icon for the documentation site and the README

### Fixed
- An unreadable configuration file was silently replaced by the defaults on the next save, losing clusters, VM services and stored credentials: it is now kept as `config.bak-<timestamp>` and the login window says so. The configuration is written to a temporary file and moved into place, so a crash mid-write no longer truncates it; on Linux and macOS the temporary and backup files are created readable only by the user (#48, thanks @skygunner)
- **View documentation** in Settings → Kiosk opened `docs/KIOSK.md` on GitHub, removed with the move to the documentation site; it now opens the Kiosk mode page. **Documentation** in the ⋮ menu opens the documentation site instead of the README
- The viewer path pointed to `virt-viewer`, installed next to `remote-viewer` by the same package, and every console opened the misleading *No running virtual machine found* dialog: cv4pve-vdi now uses the `remote-viewer` in the same folder, and warns only when it is missing. A SPICE viewer that fails to start is reported instead of ignored (#41)
- The command line of a launcher, with the password it may contain, is no longer printed to the console; the temporary Windows Credential Manager entry is removed also when the program fails to start (#44)
- **Add selected** and **Cancel** in the Discover dialog did not close it (#45)
- Editing a built-in launcher lost its icon, and changing only the icon was not saved (#46)
- A failing Start or Shutdown closed the application; now the error is shown. A failed guest agent ping no longer skips the update of that VM (#47)
- An IP address from the guest agent or the IP override that is not a host name or address is rejected before starting a launcher, so a guest cannot inject arguments into its command line (#49)
- The folder button next to the viewer path showed the raw text `SelectSpiceViewer` (#50)
- The viewer messages are translated in every language, and point to Settings → Launchers, where the viewer path is set
- Editing a cluster in Settings → Clusters deleted the services configured on its VMs, with their credentials
- **Reset all built-ins to default** in Settings → Launchers also deleted the custom launchers, although the confirmation says they are kept
- Leading and trailing spaces were removed from the password of a service with Manual credentials
- Services on a container never found its IP address: it was asked to the QEMU guest agent, which containers do not have. It now comes from the container interfaces (`VM.Audit` is enough)
- The IP address of a guest running Docker could be a Docker bridge (172.17.0.1, ...) instead of the real NIC: the interface with the MAC of a NIC configured in Proxmox VE is now used first
- The tag filter kept every guest without tags
- A node with no guest left by the filters still showed its header, and the "no results" message never appeared
- After **Switch user** the previous window stayed in memory, with its session and password, and kept checking for updates: N switches meant N update checks. The update menu item opened the release page once per check (part of #51)
- The launcher list was read from disk once per guest at every redraw (part of #51)

### Changed
- The application and window icon is the cv4pve-vdi product icon, as on the documentation site, instead of the Corsinvest logo
- Updated Corsinvest.ProxmoxVE.Api.Extension to 9.2.3
- Faster filtering on large clusters: the search box filters once typing pauses instead of on every keystroke, Reset rebuilds the view once, and a refresh redraws at most twice a second while SPICE and OS details load
- Project metadata, symbols (Source Link) and code style aligned with the other cv4pve tools

## [1.7.1] - 2026-07-30

### Fixed
- A stopped VM disappeared from the list unless its VGA was SPICE-capable (`qxl`/`spice`); a stopped VNC-only VM (e.g. `vga: virtio`) vanished while stopped SPICE VMs stayed visible. Stopped VMs you can power on are now always shown, so the Start button is reachable regardless of VGA type (#37)

## [1.7.0] - 2026-05-30

### Added
- **Open sessions panel** (#33) — a strip at the top of the main window shows every viewer you have open. Click a session to bring its window back to the front, click the × to close it. The strip hides itself when nothing is running. Particularly useful in kiosk mode, where the Windows taskbar is not available
- **Launcher icons** — RDP, SPICE, VNC, SSH and the other launchers now show a recognisable icon everywhere they appear (Settings → Launchers, the Connect menu on a VM, the services list on a VM). When you add a custom launcher you can pick its icon from a dropdown

### Changed
- Closing cv4pve-vdi or switching user now asks for confirmation if there are sessions open, and closes them cleanly so the next user doesn't inherit them
- Closing a viewer from the Open sessions panel asks the viewer to shut down gracefully first, and only force-closes it after a short timeout

## [1.6.0] - 2026-05-23

### Added
- **UI translations** with a language picker on the login window — ships with English, Italian, German, French, Spanish, Brazilian Portuguese, Russian, Polish, Dutch and Czech. See [Languages](https://corsinvest.github.io/cv4pve-vdi/languages/) for how to contribute or improve a translation
- **Group by node** (Settings → Appearance) — toggle off to render VMs/CTs as a single flat list without node headers
- **Sort by** (Settings → Appearance) — order VMs and CTs by *ID* or *Name*; both kinds are interleaved by the chosen key instead of always being rendered as CTs-then-VMs
- **Search by tag** — the search box now also matches against tag names, not just VM name/ID/description (#27)
- **Empty-state message** — when filters hide every VM, a friendly message is shown instead of a blank area (#28)
- **Keyboard shortcuts** — `Ctrl+F` focus search, `F5` refresh, `Ctrl+,` open settings (#29)
- **Clear (✕) button** on the search box and on the kiosk login-background path field

### Changed
- The **Group by node** and **Sort by** controls stay visible even in kiosk mode, so non-admin users can re-order the list without the admin password

### Fixed
- Stopped VMs/CTs were not shown when the **Stopped** status filter was ticked (#26)
- Settings, Cluster edit, Launcher edit and Main window rendered strings in the startup culture instead of the language picked at the login when the user switched language at runtime

## [1.5.0] - 2026-05-11

### Added
- **Kiosk mode** — lock down the application for thin-client and shared-workstation deployments. Full-screen login and main window, advanced settings (Launchers, Clusters, advanced Appearance) hidden behind an admin password, optional login background image for branding. See [Kiosk mode](https://corsinvest.github.io/cv4pve-vdi/kiosk/) for the full guide.
- **Switch user** — sign out and return to the login screen without restarting the application. Found in the **More** menu. Especially useful in kiosk mode where multiple people share the same thin client.
- **Admin unlock** — once the admin password is entered, the session stays unlocked until the application is closed or **Switch user** is clicked. No need to re-enter the password for each protected action.

## [1.4.2] - 2026-05-11

### Added
- **RDP single sign-on** — the **RDP (mstsc)** launcher now correctly passes credentials to Windows so you don't have to type them again. Works with domain, workgroup and local accounts
- **Discover progress indicator** — when scanning a VM for services, a progress bar now shows the operation is in progress

### Changed
- Information and error dialogs now use a single OK button (instead of confusing Yes/No) and show a coloured icon based on severity
- Launchers can declare advanced Windows Credential Manager options (credential type and target template) for custom RDP-like tools

### Fixed
- "Could not resolve IP address" is now correctly shown as an error rather than a confirmation dialog

## [1.4.1] - 2026-04-14

### Changed
- Updated Avalonia to 12.0.1, which includes a security fix for a known vulnerability in the Linux networking dependency (Tmds.DBus.Protocol)
- Internal code cleanup: removed duplicated remote viewer logic now provided by the shared library

## [1.4.0] - 2026-04-10

### Changed
- The app starts faster and feels more responsive, especially when scrolling through large VM lists
- Buttons, dropdowns and input fields look sharper and react better to hover and focus
- Dark mode appearance is more consistent across all windows

## [1.3.1] - 2026-03-28

### Added
- **Browse button for launcher executable** — click the folder icon next to the executable path to pick the file from a dialog instead of typing it manually

### Changed
- **Port fields** — port number fields now show a distinct icon, easier to tell apart from host/IP fields
- **Terminology** — "Host" renamed to "Cluster" throughout the interface

## [1.3.0] - 2026-03-26

### Added
- **Service launchers** — connect to VMs via RDP, SSH, PuTTY and any custom tool directly from the Connect button; built-in launchers for mstsc, xFreeRDP, SSH (cmd, PuTTY, GNOME Terminal, xterm, Konsole, macOS Terminal)
- **Per-VM services** — configure one or more services per VM with custom port, credentials and IP; accessible from **Connect → Services...**
- **Service discovery** — auto-detect open ports on a VM and add the matching services in one click
- **Credentials per service** — save username/password per service, or use the Windows Credential Manager on Windows

### Changed
- **Connect button** — SPICE, VNC and all configured services are now grouped in a single dropdown per VM
- **Guest agent badge** — starts gray when first enabled, turns green/red as each VM is checked (no more sudden red flash)

## [1.2.0] - 2026-03-20

### Added
- **VNC console** — connect to any running VM or container directly from the app
- **⋮ menu** — new toolbar button with links to documentation, release notes, support, bug report and feature request; Settings and About moved here
- **Update notification** — red badge on the ⋮ button and a menu entry when a new version is available
- **Default view** — choose whether the app starts in Card or List view (Settings → Appearance)
- **Warning banner** — shown when remote-viewer is not configured, with a direct link to open Settings
- **Node, Pool and Tag filters** — each filter section can be shown or hidden independently in Settings; enabling one automatically refreshes the list
- **Reset filters button** — compact × icon inline with the "FILTERS" sidebar header

### Changed
- **Faster refresh** — guest agent status is now cached and re-checked at most every 60 seconds, noticeably faster on large clusters
- **Auto-refresh** — shows a "30s" label when active; replaced internal loop with `DispatcherTimer`, ticks skipped if a refresh is already in progress
- **Settings** — display options arranged in a compact 2-column layout
- **Edit Cluster dialog** — removed Cancel button; close the window to cancel
- **VNC session title** — `.vv` file now includes a `title` field (`node:type/vmid`) visible in the remote-viewer title bar
- **Progress bar** — increased height to 8 px for better visibility
- **Settings Appearance tab** — icon updated to Palette

### Fixed
- SPICE, VNC and RDP buttons were showing as blank boxes in dark theme
- Switching between light and dark theme now correctly updates all colors in the top bar

## [1.1.0] - 2026-03-18

UX improvements, update checker, code reorganization.

### What's Changed
- chore: update README and WinGet manifest
- fix: fix ASCII logo alignment in README
- chore: add GitHub issue templates
- feat: UX improvements, update checker, code reorganization

## [1.0.0] - 2026-03-16

We are excited to announce the first public release of **cv4pve-vdi**!

After years of managing Proxmox VE clusters, we wanted a lightweight desktop client that lets you connect to VMs and containers without opening a browser. cv4pve-vdi is exactly that — a fast, cross-platform VDI launcher with SPICE and RDP support and a clean interface that stays out of your way.

Login with your Proxmox VE credentials and manage multiple clusters from a single application.

If you prefer working from the terminal, check out [**cv4pve-pepper**](https://github.com/Corsinvest/cv4pve-pepper) — our companion command-line tool for launching SPICE consoles on Proxmox VE.
