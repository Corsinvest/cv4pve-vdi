# TODO

> These are ideas and reminders — they may or may not be implemented.

## Cross-platform credential vault (Windows / macOS / Linux)

Today `CredentialSource.Manual` saves the password as plaintext in `~/.cv4pve/vdi/config` (YAML). Every serious VDI client (Citrix, VMware Horizon, AnyDesk, Slack, 1Password, …) uses the OS keychain instead. Goal: add a new `CredentialSource.SystemKeychain` that delegates to the platform vault, behind a single `ISecretStore` abstraction.

- [ ] Define `ISecretStore` with `Set/Get/Delete(string service, string account, …)`; the `service` key is `cv4pve-vdi/<vmid>/<serviceid>` so each VM-service pair has its own slot
- [ ] **Windows**: refactor the existing `Services/WindowsCredentialManager.cs` into `WindowsCredentialManagerStore : ISecretStore`. The P/Invoke for `CredRead/CredWrite/CredDelete/CredFree` is already there — main change is switching `Persist = CRED_PERSIST_SESSION` (today, transient for RDP-SSO) to `CRED_PERSIST_LOCAL_MACHINE` for the persistent storage path, and exposing a public `Get(target)` overload. The transient RDP-SSO path stays as it is
- [ ] **macOS**: write `MacKeychainStore : ISecretStore` (~80 LOC) — P/Invoke `Security.framework` `SecItemAdd/SecItemCopyMatching/SecItemDelete` over `CFDictionary` from `CoreFoundation.framework`. Items go in the user's *login* keychain (not iCloud Keychain). First access shows the standard "cv4pve-vdi wants to use confidential information" prompt — user clicks *Always Allow* once
- [ ] **Linux**: write `LinuxLibsecretStore : ISecretStore` (~60 LOC) — P/Invoke `libsecret-1.so.0` `secret_password_store_sync/lookup_sync/clear_sync` with a `SecretSchema`. Requires a Secret Service daemon (gnome-keyring on GNOME, kwallet on KDE, default on Ubuntu / Fedora desktop). On headless / WM-minimalist setups → `ISecretStore.IsAvailable == false`, fall back to the existing Manual plaintext path with a UI warning
- [ ] **Factory** `SecretStore.Default` picks the right backend by `RuntimeInformation.IsOSPlatform(...)`
- [ ] UI: add `CredentialSource.SystemKeychain` enum value, expose it in `VmServiceEditWindow` next to `Vdi / Manual / None`. When picked, password textbox is replaced by *"Stored in system keychain"* + a *Forget* button
- [ ] Migration: on first launch after upgrade, if the user has any `Manual` credentials, offer a one-click *"Move these to the system keychain"* in Settings — strictly opt-in, never automatic

### Decisions already made

- **Build vs buy**: rejected `Devlooped.CredentialManager` (cross-platform but Open Source Maintenance Fee for commercial use). Rejected `KeySharp` (unmaintained since 2022). Picked **hybrid DIY** for the Mac/Linux halves since the Windows half already exists in-tree
- **macOS signing**: Keychain ACLs bind the entry to the app's code-signing identity. Items written by a debug / unsigned build are not visible to a signed release build (and vice versa). Since the macOS release already needs notarization for Gatekeeper, this is not extra cost — just a constraint to remember when testing
- **Not equivalent to Windows RDP SSO**: on macOS, RDP clients (xFreeRDP, Microsoft Remote Desktop) don't auto-read the Keychain the way `mstsc` reads the Vault. So `SystemKeychain` on macOS gives secure storage at rest but the password still flows through CLI args / a launcher prompt — the same as `Manual` today, just no plaintext on disk
- **Estimated effort**: ~3 dev-days (~140 new lines split Mac/Linux + interface + small Windows refactor + UI) — the Windows P/Invoke being already done is the big saving

## Auto-refresh on startup

- [ ] Add a checkbox in Settings → Appearance: **"Start auto-refresh on launch"**

## OS detail in card/list view (human-readable)

- [ ] Show a friendly OS name under the VM/CT badge (e.g. `Windows 11`, `Ubuntu`, `Debian`) instead of relying only on the Linux/Windows icon
- [ ] QEMU: data already in cache (`OsType` from `Qemu.Config.GetAsync()`), just needs a code → friendly-name mapping (`win11` → `Windows 11`, `l26` → `Linux`, ...)
- [ ] LXC: requires an extra `Lxc.Config.GetAsync()` per container (currently hard-coded to "linux"); cache permanently like QEMU, optionally limit to running containers to reduce first-refresh cost
- [ ] Worth doing only if combined with a concrete use case — e.g. an "OS" filter in the sidebar — otherwise it's just decoration (the existing Linux/Windows icon already conveys the gist)

## Auto-update of the client

- [ ] Check for new releases on startup (GitHub releases API), download and offer to install in one click
- [ ] In kiosk mode the value is huge: rolling out a fix to 50 thin clients is impossible without it (today the operator has to log into each box). Outside kiosk it's mostly convenience
- [ ] Windows: signed MSI or an `.exe` self-extractor; Linux: provide an AppImage / `.deb` channel so we can ship update bits without root; macOS: signed `.pkg` or Sparkle-style differential update
- [ ] Add a Settings → Updates panel: *Check now*, *Auto-check daily*, *Stable / Beta* channel toggle
- [ ] Same pattern used by Citrix Workspace, VMware Horizon Client, AnyDesk — every big VDI client ships its own updater

## Multi-cluster unified view

- [ ] Track the discussion in #32 — the user asked to see VMs from multiple clusters in a single list
- [ ] Architectural change: today MainWindow holds a single `PveClient` + `ClusterConfig`; would become a list `(client, host)[]` and every refresh / launcher path needs to know which cluster a row belongs to
- [ ] Identity: VMID is not unique across clusters, so the row identity becomes `(ClusterName, VmId)`; filters (Nodes, Pools, Tags) merge from N clusters and need a cluster prefix where they collide
- [ ] Cheaper intermediate step: a **"Quick switch"** dropdown in the topbar that flips clusters without going back to the login window — covers the most common "I just don't want to log out" pain
- [ ] Wait for the author's reply on #32 before committing to either path

## URL handler (cv4pve-vdi://)

- [ ] Register a custom URI scheme so a link like `cv4pve-vdi://cluster=prod&vmid=100&service=rdp` opens cv4pve-vdi and launches the right service directly
- [ ] Unlocks integration with internal wikis, ticketing systems, monitoring dashboards — same pattern as `receiver://` (Citrix), `vmware-view://` (Horizon), `rdp://`, `ssh://`
- [ ] Small implementation cost on each OS: Windows registry under `HKCR\cv4pve-vdi`, Linux `.desktop` MimeType, macOS `Info.plist` CFBundleURLTypes
- [ ] Define a stable URL grammar before shipping — once external systems start using it, breaking it later is painful

## Centralised configuration provisioning (GPO / MDM / config files)

- [ ] Today the operator configures each thin-client by hand (clusters, viewer path, kiosk password, launchers); on a fleet of 50+ machines this is the bottleneck
- [ ] Read configuration from machine-wide locations in addition to the per-user `config.yaml`:
  - Windows: `HKLM\Software\Corsinvest\cv4pve-vdi` (GPO writes ADMX/ADML), `%PROGRAMDATA%\cv4pve-vdi\config.yaml`
  - Linux: `/etc/cv4pve-vdi/config.yaml`, `/etc/cv4pve-vdi/conf.d/*.yaml`
  - macOS: `/Library/Application Support/cv4pve-vdi/config.yaml`, defaults domain
- [ ] Merge order: defaults → machine → user, with **machine settings able to lock fields** (kiosk mode in particular)
- [ ] Ship an `.adml`/`.admx` pair so admins can manage cv4pve-vdi from Group Policy like Citrix Workspace does
- [ ] Document the precedence and the locked-field syntax in `docs/KIOSK.md` (already the natural home of fleet deployment notes)

## OIDC / SAML / AD login

- [ ] Track the discussion in #6 — the user asked for OIDC redirect at login (besides the current PAM / PVE / LDAP flow)
- [ ] Proxmox VE itself supports an OIDC realm (`pveum realm add ... --type openid`); the client side needs to handle the OAuth2 redirect dance: open the system browser to the IdP's authorize URL, host a tiny loopback HTTP listener (`http://127.0.0.1:<random>/callback`) to receive the code, exchange it for a token, then call the PVE API with that token
- [ ] LDAP and AD are already covered by PVE realms — the client just sends username/password to the right realm; no client work needed today
- [ ] SAML is not currently a PVE-native realm — would require a SAML library on the client side; lower priority than OIDC
- [ ] Touches `cv4pve-api-dotnet`: the auth helpers there assume username/password, would need a token-based code path. Coordinate the change across both repos
- [ ] Worth doing once a concrete enterprise deployment asks for it — the design is well-known but the test matrix (Keycloak, Azure AD, Auth0, Authentik, …) is the real cost

## Session log / local audit

- [ ] Append-only log under `%LOCALAPPDATA%\cv4pve-vdi\sessions.log` (or platform equivalent): timestamp, user, cluster, VMID, service, exit code, duration
- [ ] Settings → Privacy toggle: *Keep session history* (default on outside kiosk, off in kiosk unless the operator opts in)
- [ ] Useful for the user themselves ("when did I last connect to web01?") and for auditing in regulated environments
- [ ] No telemetry going off the box — strictly local. Aggregation / shipping is out of scope for the client


