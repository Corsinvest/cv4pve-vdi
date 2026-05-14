# Settings

Open **Settings** from the *More* menu (three dots, top-right). The settings window is split into four tabs: **Appearance**, **Launchers**, **Clusters** and **Kiosk**.

| Appearance | Launchers | Clusters | Kiosk |
|------------|-----------|----------|-------|
| ![Appearance](images/settings-appearance.png) | ![Launchers](images/settings-launchers.png) | ![Clusters](images/settings-clusters.png) | ![Kiosk](images/settings-kiosk.png) |

## Appearance tab

Controls how the VM list and card view look, plus a few behavioural toggles.

| Setting | Description |
|---------|-------------|
| **Theme** | Light / Dark / System |
| **Default view** | Card or List view at startup |
| **Show CPU/RAM bars** | Toggle resource usage bars in card and list view |
| **Show nodes filter** | Show the node filter section in the sidebar |
| **Show pools** | Show the pool filter in the sidebar and pool info in cards |
| **Show tags** | Show tag badges in cards/list and the tag filter in the sidebar |
| **Ping guest agent** | Ping the QEMU guest agent to show a live status badge (green/red). Adds one API call per running QEMU VM, so on large clusters consider leaving it off |
| **Show Start button** | Show the power-on button per VM in card/list actions |
| **Show Shutdown button** | Show the shutdown button per VM in card/list actions |
| **Ask confirmation** | Confirm before Start / Shutdown |

> [!NOTE]
> In **kiosk mode** non-admin users see only the **Theme** and **Default view** options on this tab. Everything else is hidden until the admin unlocks the session — see [docs/KIOSK.md](KIOSK.md).

## Launchers tab

Manages the list of service launchers available on this machine. Built-in launchers are loaded from the embedded `launchers.yaml` and cover the most common tools per platform (RDP, SSH, PuTTY, ...). You can add custom launchers, edit existing ones or remove them — changes are saved as overrides in the user configuration.

![Edit launcher](images/edit-launcher.png)

Launcher definition fields (see the [Service Launchers](../README.md#service-launchers) section in the README for the argument token reference):

| Field | Description |
|-------|-------------|
| **Service ID** | Unique identifier (e.g. `rdp-mstsc`, `ssh-putty-windows`) — read-only on built-in launchers |
| **Display name** | Shown in the Connect menu |
| **Platform** | Windows / Linux / macOS — only launchers matching the current platform are shown |
| **Default port** | Pre-filled when adding a new service of this kind |
| **Executable** | Path to the program (Browse button opens a file picker) |
| **Arguments** | Command-line template with token substitution (`{ip}`, `{port}`, `{username}`, `{password}`, `{extraArgs}`, `{?...}`) |
| **Extra arguments** | Default extra arguments, overridable per-service |
| **Supports credentials** | Whether the tool can accept username/password |
| **Use Windows Credential Manager** | (Windows only) Inject credentials into the Vault before launching — required by `mstsc` for RDP single sign-on. Shows additional fields for credential type and Vault target template |

## Clusters tab

Defines the Proxmox VE clusters cv4pve-vdi can connect to. Each cluster has one or more API endpoints (for HA failover), TLS options and SPICE settings.

![Edit cluster](images/edit-cluster.png)

| Setting | Description |
|---------|-------------|
| **Name** | Friendly name shown in the cluster picker on the login window |
| **Host** | Proxmox VE API endpoint(s). Accepts a comma-separated list for HA failover (`host1:port,host2:port`) — the first reachable host is used |
| **Skip TLS validation** | Disable certificate check (useful for self-signed certs) |
| **Timeout** | API connection timeout in seconds |
| **SPICE proxy** | Optional override for the SPICE proxy. By default the cluster API host is used; set this if you need a different reverse-proxy URL for SPICE traffic |
| **Viewer extra options** | Additional command-line arguments passed to `remote-viewer` for sessions on this cluster |

> [!NOTE]
> The **SPICE viewer path** (path to `remote-viewer`) is configured separately on the **Launchers tab** — it's a per-machine setting, not per-cluster.

## Kiosk tab

Configures kiosk / thin-client mode. When enabled, the application starts full-screen, advanced settings are hidden from regular users, and an admin password is required to unlock protected configuration.

![Kiosk tab](images/settings-kiosk.png)

| Setting | Description |
|---------|-------------|
| **Enable kiosk mode** | Master toggle. When off, everything else on this tab is ignored |
| **Force full-screen** | When on (recommended), forces both the login and main windows to open in full-screen. Turn off if your OS shell replacement / window manager handles sizing externally |
| **Admin password** | PBKDF2-SHA256 hashed and saved to `config.yaml`. Leave both password fields empty when re-saving to keep the existing password |
| **Login background image** | Optional image (PNG, JPG, BMP, GIF) shown behind the centered login form when kiosk mode is active. Useful for branding the thin client |

Full kiosk guide — including deployment patterns (Windows Assigned Access, Linux session replacement) — in **[docs/KIOSK.md](KIOSK.md)**.
