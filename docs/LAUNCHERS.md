# Service Launchers

cv4pve-vdi can launch **any external tool** against a VM — RDP, SSH, PuTTY, and anything else with a command-line interface — through a two-layer system:

- **Launchers** describe *how* to invoke an external program (executable, arguments template, credential handling). Managed in **Settings → Launchers**.
- **[Services](SERVICES.md)** map a launcher to a specific VM and override port, credentials, IP or extra arguments. Managed per-VM via **Connect → Services...**.

This page documents the launchers themselves. See **[docs/SERVICES.md](SERVICES.md)** for the per-VM side.

## Built-in launchers

| Launcher | Platform | Default port |
|----------|----------|------|
| RDP (mstsc) | Windows | 3389 |
| RDP (xFreeRDP) | Windows / Linux / macOS | 3389 |
| SSH (cmd) | Windows | 22 |
| SSH (PuTTY) | Windows / Linux / macOS | 22 |
| SSH (GNOME Terminal) | Linux | 22 |
| SSH (xterm) | Linux | 22 |
| SSH (Konsole) | Linux | 22 |
| SSH (Terminal) | macOS | 22 |

Only launchers matching the current platform are shown in the **Connect** menu.

Built-in launchers come from the embedded `launchers.yaml`. You can override any of their fields, or add entirely new launchers, in **Settings → Launchers** — overrides are stored in your user configuration and merged on top of the built-ins at startup.

![Edit launcher](images/edit-launcher.png)

## Launcher fields

| Field | Description |
|-------|-------------|
| **Service ID** | Unique identifier (e.g. `rdp-mstsc`, `ssh-putty-windows`). Read-only on built-in launchers |
| **Display name** | Shown in the Connect menu |
| **Platform** | Windows / Linux / macOS — only matching launchers are visible at runtime |
| **Default port** | Pre-filled when adding a new service of this kind |
| **Executable** | Path to the program (Browse button opens a file picker) |
| **Arguments** | Command-line template with token substitution — see [Argument tokens](#argument-tokens) |
| **Extra arguments** | Default extras appended to the template, overridable per-service |
| **Supports credentials** | Whether the tool can accept username/password from cv4pve-vdi |
| **Use Windows Credential Manager** | (Windows only) Inject credentials into the Vault before launching. Required by `mstsc` for RDP single sign-on — see [SERVICES.md](SERVICES.md#single-sign-on-for-rdp-windows) |

When **Use Windows Credential Manager** is on, two additional fields are revealed:

| Field | Description |
|-------|-------------|
| **Credential type** | `Generic` (most apps, e.g. Git) or `DomainPassword` (required by `mstsc` for RDP) |
| **Target template** | Vault entry name written before launch. Supports `{ip}`. Example for RDP: `TERMSRV/{ip}` |

## Argument tokens

The `arguments` field is a template — cv4pve-vdi substitutes the following tokens before launching:

| Token | Description |
|-------|-------------|
| `{ip}` | VM IP address (resolved via QEMU guest agent or from the service's IP override) |
| `{port}` | Port number — empty when equal to the launcher default, so optional flags can be omitted |
| `{username}` | Username (from the credential source) |
| `{password}` | Password (from the credential source) |
| `{extraArgs}` | Extra arguments (per-service override, falling back to the launcher's default) |
| `{?TEXT}` | Conditional: include `TEXT` (with its inner tokens resolved) only if all tokens inside it are non-empty |

The `{?...}` conditional is what makes templates clean: command-line flags like `-p PORT` or `/u:USER` are only emitted when the corresponding token has a value.

Examples taken from the built-in launchers:

```
mstsc:    /v:{ip}{?::{port}} {extraArgs}
          # Adds ":port" only when port differs from the default (3389)

xFreeRDP: /v:{ip}{?::{port}} /cert:ignore {?/u:{username}} {?/p:{password}} {extraArgs}
          # Skips /u and /p when no credentials are provided

PuTTY:    -ssh {ip} {?-P {port}} {?-l {username}} {?-pw {password}} {extraArgs}

SSH cmd:  /k ssh {?-p {port}} {?{username}@}{ip} {extraArgs}
          # cmd /k keeps the window open after ssh exits;
          # "user@" prefix only when a username is provided
```

## Adding a custom launcher

Click **Add** in **Settings → Launchers** and fill in the fields above. A few tips:

- Pick a **unique Service ID** — it's the key used to merge user overrides on top of the built-ins
- Set **Platform** to the current OS — only launchers matching the current platform appear in the Connect menu
- If your tool reads credentials from the **Windows Credential Manager**, enable that flag and set the appropriate target template (see [Single Sign-On for RDP](SERVICES.md#single-sign-on-for-rdp-windows) for the mstsc case)
- Use the `{?...}` conditional generously: it keeps the command-line clean when port/credentials are not set

The custom launcher is saved to the user configuration; built-in launchers can be edited the same way and your changes are stored as overrides.
