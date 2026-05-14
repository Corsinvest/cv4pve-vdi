# Guest Setup

To get the most out of cv4pve-vdi, a few components need to be installed **inside the VM** (the guest). Nothing here is required to *connect* to a VM, but each piece unlocks an additional feature.

## QEMU Guest Agent

The guest agent lets Proxmox query the running OS for information like network interfaces and process state. cv4pve-vdi uses it for:

- The **agent status badge** on VM cards/rows (green = running, red = not responding, gray = unknown / ping disabled)
- **Auto-resolving the VM IP** when launching a service (RDP, SSH, ...) without an IP override

Refer to the official Proxmox documentation for the setup:

- [Proxmox VE Wiki — Qemu-guest-agent](https://pve.proxmox.com/wiki/Qemu-guest-agent)

> [!NOTE]
> The agent badge is only shown on QEMU VMs (not LXC containers). It stays gray until you enable **Ping guest agent** in Settings → Appearance — once enabled, each running VM is pinged and the badge turns green or red.

## SPICE features (audio, USB, clipboard)

SPICE advanced features (audio passthrough, USB redirection, clipboard / folder sharing) require the VM display to be set to **SPICE** in Proxmox VE hardware settings *and* the SPICE guest tools installed inside the VM. Refer to the official documentation for installation:

- [Proxmox VE Wiki — SPICE](https://pve.proxmox.com/wiki/SPICE)
- [SPICE — Download](https://www.spice-space.org/download.html) (Windows guest tools, Linux packages)

Once the tools are installed and the VM is running, cv4pve-vdi shows the corresponding badges on the VM card/row:

| Badge | Meaning |
|-------|---------|
| 🔊 **Audio** | SPICE audio device configured |
| 🔌 **USB** | SPICE USB redirect configured |
| 📋 **Clipboard** | SPICE clipboard / folder sharing configured |

The badges are computed from the VM hardware configuration, so they show up even when the VM is stopped — they tell you *what's available*, not *what's currently active*.
