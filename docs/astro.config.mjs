// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-vdi',
  integrations: [
    starlight({
      title: 'cv4pve-vdi',
      description: 'Desktop VDI client for Proxmox VE: SPICE, VNC, RDP and SSH to your VMs and containers, without the web UI.',
      // Brand, product icon, GitHub link, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-vdi',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 7 },
          // Steps panel in the home hero: the sections of Getting started, in the same order and words,
          // each linked to its section. cv4pve-vdi is a desktop application (packaging/config: type=gui):
          // its first steps are windows and screenshots, so that page has sections, not numbered steps.
          steps: {
            items: [
              { text: 'Install cv4pve-vdi', href: 'getting-started/#install-cv4pve-vdi' },
              { text: 'Install remote-viewer', href: 'getting-started/#install-remote-viewer' },
              { text: 'Add your cluster', href: 'getting-started/#add-your-cluster' },
              { text: 'Log in', href: 'getting-started/#log-in' },
            ],
          },
        }),
      ],
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
