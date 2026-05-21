# TODO

> These are ideas and reminders — they may or may not be implemented.

## macOS Keychain credential storage

- [ ] Use macOS Keychain (via P/Invoke on `Security.framework`) as a secure credential store, equivalent to Windows Credential Manager
- [ ] Useful for encrypting saved Manual credentials (today stored in plaintext in `config.yaml`)
- [ ] **Not** equivalent to the Windows RDP single sign-on feature: macOS RDP clients (xFreeRDP, Microsoft Remote Desktop) don't auto-read the Keychain the way `mstsc` reads the Windows Vault — they expect credentials as CLI arguments or via their own native UI

## Linux libsecret credential storage

- [ ] Use libsecret (GNOME Keyring / KWallet) as optional credential source on Linux
- [ ] Only available if `libsecret-1-0` is installed and a secrets daemon is running (GNOME/KDE)
- [ ] Fall back to plaintext + `chmod 600` if libsecret is not available

## Auto-refresh on startup

- [ ] Add a checkbox in Settings → Appearance: **"Start auto-refresh on launch"**

## OS detail in card/list view (human-readable)

- [ ] Show a friendly OS name under the VM/CT badge (e.g. `Windows 11`, `Ubuntu`, `Debian`) instead of relying only on the Linux/Windows icon
- [ ] QEMU: data already in cache (`OsType` from `Qemu.Config.GetAsync()`), just needs a code → friendly-name mapping (`win11` → `Windows 11`, `l26` → `Linux`, ...)
- [ ] LXC: requires an extra `Lxc.Config.GetAsync()` per container (currently hard-coded to "linux"); cache permanently like QEMU, optionally limit to running containers to reduce first-refresh cost
- [ ] Worth doing only if combined with a concrete use case — e.g. an "OS" filter in the sidebar — otherwise it's just decoration (the existing Linux/Windows icon already conveys the gist)


