# VNC via Internal Bridge

cv4pve-vdi offers a **VNC console** on every running QEMU VM and LXC container — **without** opening firewall ports on the Proxmox nodes, without exposing the per-VM VNC server to the network, and without requiring any extra software beyond `remote-viewer`.

This is possible because cv4pve-vdi never talks plain VNC to the node. It rides on top of the **same Proxmox API connection** the application already uses, tunnels VNC through a **WebSocket**, and presents the stream to `remote-viewer` via a tiny **local TCP bridge**.

## Connection flow

When the user clicks **Connect → VNC** on a VM, cv4pve-vdi performs the following sequence:

```
   cv4pve-vdi                       Proxmox VE node            Guest VM
   ──────────                       ────────────────            ────────
       │                                  │                        │
       │  1. POST /vncproxy?websocket=1   │                        │
       ├─────────────────────────────────►│                        │
       │      (uses existing API auth)    │                        │
       │                                  │                        │
       │  2. ticket + port (one-shot)     │                        │
       │◄─────────────────────────────────┤                        │
       │                                  │                        │
       │  3. WebSocket (wss) over the same TLS port used by the API│
       ├═════════════════════════════════►│                        │
       │                                  │                        │
       │                                  │  4. relays to VM's     │
       │                                  │     VNC server         │
       │                                  ├───────────────────────►│
       │                                  │                        │
```

1. The application asks the Proxmox API for a one-shot **VNC ticket** and **TCP port** for the target VM/CT.
2. Proxmox replies with a short-lived ticket valid only for that VM and port.
3. cv4pve-vdi opens a **WebSocket** connection (`wss://`) to the node, authenticated with the same cookie used for the API call. No new ports are opened on the Proxmox side — the WebSocket runs on the standard `pveproxy` TLS port (default 8006).
4. Proxmox internally relays the WebSocket to the VM's VNC server.

## Local bridge

`remote-viewer` (the standard SPICE/VNC client we already ship configuration for) speaks **plain VNC over TCP**, not WebSocket. To bridge the two, cv4pve-vdi spins up an **in-process bridge** on `127.0.0.1` and a random local port:

```
                     ┌────────────────────────────────────────┐
                     │              cv4pve-vdi                │
                     │                                        │
                     │       ┌────────────────────────┐       │
                     │       │      bridge-vnc        │       │
   remote-viewer     │       │                        │       │       Proxmox
   (plain VNC) ─────►│  TCP  │  127.0.0.1:<rand>  ◄══►│  WS  ◄│══════►  node
                     │       │                        │       │       (wss)
                     │       │  plain VNC ⇄ WebSocket │       │
                     │       └────────────────────────┘       │
                     └────────────────────────────────────────┘
                                local in-process bridge
```

cv4pve-vdi then writes a temporary `.vv` file pointing `remote-viewer` at `127.0.0.1:<random-port>` with the one-shot ticket as password, and launches the viewer. When the viewer exits, the bridge is torn down.

## What this means

- **No direct network access** to the per-VM VNC port on the node is required
- **No firewall rules** need to be opened on the Proxmox host beyond the standard API port
- **No extra software** to install: the same `remote-viewer` used for SPICE handles VNC too
- **Works on every running VM and container**, regardless of the display hardware configuration
- The session is **authenticated** through the existing Proxmox VE login — no separate VNC password to manage

> [!NOTE]
> Because the bridge listens on `127.0.0.1`, only processes on the local machine can connect to it. The random port and one-shot ticket make even local hijacking impractical.
