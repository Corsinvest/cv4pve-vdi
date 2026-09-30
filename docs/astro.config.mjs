// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

const releases = 'https://github.com/Corsinvest/cv4pve-vdi/releases/latest/download';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-vdi',
  integrations: [
    starlight({
      title: 'cv4pve-vdi',
      description: 'Desktop VDI client for Proxmox VE: SPICE, VNC, RDP and SSH to your VMs and containers, without the web UI.',
      // Brand, logo, GitHub and "Edit page" links, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-vdi',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 7 },
          // Install-and-start panel in the home hero. cv4pve-vdi is a desktop application
          // (packaging/config: type=gui): it ships the release zips and WinGet only, and it
          // is started without arguments, so the targets are written out instead of the presets.
          install: {
            targets: [
              {
                id: 'windows',
                label: 'Windows',
                icon: 'windows',
                lines: [
                  '# install',
                  'winget install Corsinvest.cv4pve.vdi',
                  '',
                  '# viewer for SPICE and VNC',
                  'winget install RedHat.VirtViewer',
                  '',
                  '# start',
                  'cv4pve-vdi',
                ],
              },
              {
                id: 'linux',
                label: 'Linux',
                icon: 'linux',
                lines: [
                  '# install (x64; arm64 and arm on the Releases page)',
                  `wget ${releases}/\\\ncv4pve-vdi-linux-x64.zip`,
                  'unzip cv4pve-vdi-linux-x64.zip',
                  'chmod +x cv4pve-vdi',
                  '',
                  '# viewer for SPICE and VNC',
                  'sudo apt install virt-viewer',
                  '',
                  '# start',
                  './cv4pve-vdi',
                ],
              },
              {
                id: 'macos',
                label: 'macOS',
                icon: 'macos',
                lines: [
                  '# install (Apple silicon; Intel: osx-x64)',
                  `curl -LO ${releases}/\\\ncv4pve-vdi-osx-arm64.zip`,
                  'unzip cv4pve-vdi-osx-arm64.zip',
                  'chmod +x cv4pve-vdi',
                  '',
                  '# start',
                  './cv4pve-vdi',
                ],
              },
            ],
          },
        }),
      ],
      lastUpdated: true,
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'how-it-connects', 'troubleshooting'],
        },
        {
          label: 'Using cv4pve-vdi',
          items: ['main-window', 'consoles', 'services', 'launchers', 'guest-setup'],
        },
        {
          label: 'Deployment',
          items: ['kiosk'],
        },
        {
          label: 'Reference',
          items: ['settings', 'languages'],
        },
      ],
    }),
  ],
});
