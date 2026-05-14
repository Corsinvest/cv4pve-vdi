# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.5.0] - 2026-05-11

### Added
- **Kiosk mode** — lock down the application for thin-client and shared-workstation deployments. Full-screen login and main window, advanced settings (Launchers, Clusters, advanced Appearance) hidden behind an admin password, optional login background image for branding. See [docs/KIOSK.md](docs/KIOSK.md) for the full guide.
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
