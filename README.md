# cv4pve-vdi

```
   ______                _                      __
  / ____/___  __________(_)___ _   _____  _____/ /_
 / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
/ /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
\____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

VDI client for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-vdi.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-vdi.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-vdi/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-vdi/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-vdi/releases)

---

## Overview

**cv4pve-vdi** is a desktop VDI client for [Proxmox VE](https://www.proxmox.com/en/proxmox-virtual-environment). It provides a graphical interface to browse, filter and connect to virtual machines and containers via **SPICE**, **VNC** and **custom service launchers** (RDP, SSH, and any other tool) — without opening the Proxmox web UI.

![Theme light and dark](docs/images/main-theme.png)

| Login | Card view | List view |
|-----------|-----------|-----------|
| ![Login](docs/images/login.png) | ![Card view](docs/images/main-card.png) | ![List view](docs/images/main-list.png) |

---

## Quick Start

Download the latest release from the [releases page](https://github.com/Corsinvest/cv4pve-vdi/releases), extract and run:

```bash
./cv4pve-vdi
```

---

## Features

### Core Capabilities

- **Card and list view** — switch between a visual card layout and a compact list
- **SPICE** console launch via `remote-viewer`
- **VNC** console via internal WebSocket bridge — no firewall rules or node-side configuration required (see [docs/BRIDGE-VNC.md](docs/BRIDGE-VNC.md))
- **Custom service launchers** — RDP, SSH, PuTTY and any other tool (see [docs/LAUNCHERS.md](docs/LAUNCHERS.md))
- **Per-VM services** — multiple connections per VM with auto-discovery and RDP single sign-on (see [docs/SERVICES.md](docs/SERVICES.md))
- **Kiosk mode** — lock down the application for thin-client and shared-workstation deployments (see [docs/KIOSK.md](docs/KIOSK.md))
- **Switch user** — sign out and return to the login screen without restarting the app (from the **More** menu)
- **VM/CT power control** — Start and Shutdown buttons (with optional confirmation)
- **Real-time stats** — CPU and RAM usage bars per VM
- **Auto-refresh** every 30 seconds — toggle from the toolbar
- **Filter sidebar** — filter by node, pool, status, type and tags
- **Tag support** — color-coded badges with Proxmox VE tag colors
- **Multi-host** — manage multiple Proxmox VE clusters from a single client
- **Theme support** — Light and Dark themes
- **Configurable** — full settings UI for appearance, launchers, clusters and kiosk (see [docs/SETTINGS.md](docs/SETTINGS.md))

### VM Badges and Indicators

Each VM card and list row shows visual indicators:

| Badge | Description |
|-------|-------------|
| 🟢 **Running** dot | VM is running |
| ⚫ **Stopped** dot | VM is stopped |
| **VM / CT / Node** badge | Resource type with OS icon |
| 🟢🔴⚫ **Agent** icon | QEMU guest agent status: green = running, red = not responding, gray = ping disabled or unknown |
| 🔊 **Audio** icon | SPICE audio device configured |
| 🔌 **USB** icon | SPICE USB redirect configured |
| 📋 **Clipboard** icon | SPICE clipboard sharing configured |
| **Tag** badges | Proxmox VE tags with color |
| **CPU / RAM** bars | Real-time resource usage |

### Guest setup

A few features (SPICE audio/USB/clipboard, QEMU guest agent badge, auto-resolving the VM IP) require components installed **inside the VM**. See **[docs/GUEST-SETUP.md](docs/GUEST-SETUP.md)** for the prerequisites.

> [!NOTE]
> Only VMs and containers with **actionable VDI capabilities** are shown — running VMs with SPICE/VNC/services available, or stopped VMs configured for SPICE display (qxl/spice). SPICE and service checks run in parallel batches with caching to keep refreshes fast on large clusters.

---

## Installation

<details>
<summary><strong>Permissions Required</strong></summary>

| Permission | Purpose |
|------------|---------|
| `VM.Console` | Launch SPICE and VNC consoles |
| `VM.PowerMgmt` | Start / Shutdown VMs |
| `VM.Audit` | Read VM configuration and status |
| `VM.Monitor` | QEMU guest agent interaction (agent ping, IP detection for services) |
| `Sys.Console` | Launch node shell (SPICE) |

</details>

### Linux Installation

```bash
# Check available releases and get the specific version number
# Visit: https://github.com/Corsinvest/cv4pve-vdi/releases

# Download specific version (replace VERSION with actual version like v1.0.0)
wget https://github.com/Corsinvest/cv4pve-vdi/releases/download/VERSION/cv4pve-vdi-linux-x64.zip

# Alternative: Get latest release URL programmatically
LATEST_URL=$(curl -s https://api.github.com/repos/Corsinvest/cv4pve-vdi/releases/latest | grep browser_download_url | grep linux-x64 | cut -d '"' -f 4)
wget "$LATEST_URL"

# Extract and make executable
unzip cv4pve-vdi-linux-x64.zip
chmod +x cv4pve-vdi
./cv4pve-vdi
```

### Windows Installation

**Option 1: WinGet (Recommended)**
```powershell
# Install using Windows Package Manager
winget install Corsinvest.cv4pve.vdi
```

**Option 2: Manual Installation**
```powershell
# Check available releases at: https://github.com/Corsinvest/cv4pve-vdi/releases
# Download specific version (replace VERSION with actual version)
Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-vdi/releases/download/VERSION/cv4pve-vdi-win-x64.zip" -OutFile "cv4pve-vdi.zip"

# Extract
Expand-Archive cv4pve-vdi.zip -DestinationPath "C:\Tools\cv4pve-vdi"
```

### macOS Installation

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-vdi/releases
# Download specific version (replace VERSION with actual version)

# Apple Silicon (arm64)
wget https://github.com/Corsinvest/cv4pve-vdi/releases/download/VERSION/cv4pve-vdi-osx-arm64.zip
unzip cv4pve-vdi-osx-arm64.zip

# Intel (x64)
wget https://github.com/Corsinvest/cv4pve-vdi/releases/download/VERSION/cv4pve-vdi-osx-x64.zip
unzip cv4pve-vdi-osx-x64.zip

chmod +x cv4pve-vdi
./cv4pve-vdi
```

---

## SPICE Client Setup

A SPICE viewer (`remote-viewer`) must be installed to use SPICE and VNC consoles.

<details>
<summary><strong>Linux (Debian/Ubuntu)</strong></summary>

```bash
sudo apt-get install virt-viewer
```

**Path**: `/usr/bin/remote-viewer`

</details>

<details>
<summary><strong>Linux (RHEL/Fedora)</strong></summary>

```bash
sudo dnf install virt-viewer
```

**Path**: `/usr/bin/remote-viewer`

</details>

<details>
<summary><strong>Windows</strong></summary>

Download from [SPICE Space](https://www.spice-space.org/download.html)

**Typical path**: `C:\Program Files\VirtViewer v?-???\bin\remote-viewer.exe`

</details>

<details>
<summary><strong>macOS</strong></summary>

Download from [SPICE Space macOS Client](https://www.spice-space.org/osx-client.html)

</details>

---

## Troubleshooting

<details>
<summary><strong>VM not visible in the list</strong></summary>

VMs are only shown if they have at least one actionable VDI capability:
- Running VM with SPICE active
- Running VM with at least one service configured
- Stopped VM with SPICE display configured (qxl or spice in hardware settings)

Check the VM's display hardware in Proxmox VE → Hardware → Display → set to **SPICE (qxl)**, or configure a service for the VM via **Connect → Services...**.

</details>

<details>
<summary><strong>SPICE launch fails</strong></summary>

- Verify the SPICE viewer path in Settings → Viewer
- Ensure `remote-viewer` is installed
- Check that the VM display is set to SPICE (qxl) in Proxmox VE hardware settings

</details>

<details>
<summary><strong>Connect button has no items</strong></summary>

The Connect dropdown shows SPICE, VNC and any configured services. If it appears empty:
- Ensure SPICE or VNC are enabled in Settings → Viewer
- Configure services for the VM via **Connect → Services...**

</details>

<details>
<summary><strong>Service not launching (RDP, SSH, etc.)</strong></summary>

- Verify the launcher executable path is correct and the tool is installed
- Ensure the VM's guest agent is active so its IP can be resolved, or set an IP override in the service configuration
- Check that the port is reachable from your machine

</details>

<details>
<summary><strong>Agent badge not showing or always gray</strong></summary>

- The agent badge is only visible on QEMU VMs (not LXC containers)
- The badge is shown if the agent is configured in Proxmox VE VM Options, but stays gray until **Ping guest agent** is enabled in Settings → Viewer
- Enable **Ping guest agent (QEMU only)** in Settings → Viewer to get live green/red status
- Ensure `qemu-guest-agent` is installed and running inside the VM

</details>

<details>
<summary><strong>SPICE audio / clipboard / USB redirect not working</strong></summary>

These features require:
1. VM display set to **SPICE** in Proxmox VE → Hardware → Display
2. **Windows**: install [SPICE Guest Tools](https://www.spice-space.org/download.html)
3. **Linux**: install `spice-vdagent` and `spice-webdavd`

The badges (audio/USB/clipboard icons) appear on the VM card only if the corresponding hardware is configured in Proxmox VE.

</details>


---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

If you prefer working from the terminal, check out [**cv4pve-pepper**](https://github.com/Corsinvest/cv4pve-pepper) — the command-line companion for launching SPICE consoles on Proxmox VE.

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
