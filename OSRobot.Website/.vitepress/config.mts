// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { readFileSync } from "node:fs";
import { defineConfig } from "vitepress";
import { buildSidebar } from "../scripts/sync-manual.mjs";
import { markdownOptions } from "./markdownOptions.mjs";

const _siteUrl = "https://www.os-robot.com";
const _manualReadme = readFileSync(new URL("../../docs/user-manual/README.md", import.meta.url), "utf8");

// Pages live in x/index.md so that URLs keep the trailing slash WordPress gave them:
// the slash is part of what search engines have indexed.
function _canonicalPath(relativePath: string): string {
  return relativePath.replace(/(^|\/)index\.md$/, "$1").replace(/\.md$/, ".html");
}

export default defineConfig({
  lang: "en-US",
  title: "OS-Robot",
  titleTemplate: ":title – OS-Robot",
  description:
    "OS-Robot is a free, open-source and cross-platform automation server with a graphical job editor.",
  cleanUrls: false,
  srcExclude: ["README.md"],
  sitemap: { hostname: _siteUrl },
  markdown: markdownOptions,
  head: [["link", { rel: "icon", href: "/favicon.jpeg" }]],
  transformPageData(pageData) {
    pageData.frontmatter.head ??= [];
    pageData.frontmatter.head.push([
      "link",
      { rel: "canonical", href: `${_siteUrl}/${_canonicalPath(pageData.relativePath)}` },
    ]);
  },
  themeConfig: {
    logo: "/favicon.jpeg",
    // The moved pages use only h3 headings, which the default outline would leave out.
    outline: { level: [2, 3] },
    nav: [
      { text: "Downloads", link: "/downloads/" },
      { text: "Installation", link: "/installation/" },
      { text: "Quick start", link: "/quick-start/" },
      { text: "Manual", link: "/manual/" },
    ],
    sidebar: {
      "/manual/": [
        { text: "User manual", link: "/manual/", items: buildSidebar(_manualReadme) },
        // The theme hides the footer on pages with a sidebar, so the legal link has to be here too.
        { text: "About", items: [{ text: "Privacy and terms", link: "/privacy-terms-of-use/" }] },
      ],
    },
    socialLinks: [{ icon: "github", link: "https://github.com/gianx1980/OSRobot" }],
    search: { provider: "local" },
    footer: {
      message: '<a href="/privacy-terms-of-use/">Privacy and terms</a>',
      copyright: "Copyright © 2025-2026 | OS-Robot.com",
    },
  },
});
