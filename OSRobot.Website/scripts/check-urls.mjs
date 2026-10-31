// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Checks the published site against every URL that has to keep working, because a
// broken URL costs search ranking. Run it right after each upload: `npm run check-urls`.
// Redirects use HEAD so that, while WordPress is live, no download gets counted or sent.

import { readdirSync, readFileSync } from "node:fs";
import { join, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { slugFor } from "./sync-manual.mjs";

const _site = "https://www.os-robot.com";
const _pages = {
  "/": "OS-Robot",
  "/downloads/": "Downloads – OS-Robot",
  "/installation/": "Installation – OS-Robot",
  "/quick-start/": "Quick start – OS-Robot",
  "/privacy-terms-of-use/": "Privacy &amp; terms of use – OS-Robot",
};
const _hiddenPaths = [
  "/wp-login.php",
  "/xmlrpc.php",
  "/wp-admin/",
  "/wp-content/plugins/download-monitor/download-monitor.php",
  "/wp-content/",
  "/wp-content/index.php",
  "/index.php.off",
];

const _websiteDir = fileURLToPath(new URL("..", import.meta.url));
const _failures = [];

function _fail(url, problem) {
  _failures.push(`${url}: ${problem}`);
}

function _filesUnder(dir) {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) =>
    entry.isDirectory() ? _filesUnder(join(dir, entry.name)) : [join(dir, entry.name)],
  );
}

async function _checkRedirect(url, target, { prefixOnly = false } = {}) {
  const response = await fetch(url, { method: "HEAD", redirect: "manual" });
  const location = response.headers.get("location") ?? "";
  const matches = prefixOnly ? location.startsWith(target) : location === target;
  if (response.status !== 301 || !matches) {
    _fail(url, `expected 301 to ${target}, got ${response.status} ${location}`);
  }
}

async function _checkStatus(url, method, expected) {
  const response = await fetch(url, { method, redirect: "manual" });
  if (response.status !== expected) {
    _fail(url, `expected ${expected}, got ${response.status}`);
  }
  return response;
}

// A partial upload leaves pages with the right status and title but no styles or scripts,
// so the build's own files have to be checked too.
export function publishedAssets() {
  const distDir = join(_websiteDir, ".vitepress", "dist");
  return _filesUnder(join(distDir, "assets")).map(
    (file) =>
      `/${relative(distDir, file)
        .split(sep)
        .map((segment) => encodeURIComponent(segment))
        .join("/")}`,
  );
}

export function expandRedirects(htaccess) {
  const redirects = [];
  for (const line of htaccess.split(/\r?\n/)) {
    const rule = line.trim().match(/^RedirectMatch\s+301\s+\^(\S+)\s+(\S+)$/);
    if (!rule || rule[1].includes("(")) {
      continue;
    }
    const [, pattern, target] = rule;
    const path = pattern.replace(/\\\./g, ".");
    if (path.endsWith("/?$")) {
      const base = path.slice(0, -"/?$".length);
      redirects.push({ path: base, target }, { path: `${base}/`, target });
    } else if (path.endsWith("$")) {
      redirects.push({ path: path.slice(0, -1), target });
    } else {
      redirects.push({ path: `${path}uncategorized/`, target });
    }
  }
  return redirects;
}

async function _run() {
  const redirects = expandRedirects(readFileSync(join(_websiteDir, "public", ".htaccess"), "utf8"));
  const chapters = readdirSync(join(_websiteDir, "..", "docs", "user-manual"))
    .filter((file) => file.endsWith(".md"))
    .map((file) => `/manual/${slugFor(file) ? `${slugFor(file)}/` : ""}`);
  const images = _filesUnder(join(_websiteDir, "public", "wp-content")).map((file) =>
    file.slice(join(_websiteDir, "public").length).replaceAll("\\", "/"),
  );

  for (const [path, title] of Object.entries(_pages)) {
    const response = await _checkStatus(_site + path, "GET", 200);
    if (!(await response.text()).includes(`<title>${title}</title>`)) {
      _fail(_site + path, `title is not "${title}"`);
    }
    if (path !== "/") {
      await _checkRedirect(_site + path.slice(0, -1), _site + path);
    }
  }

  for (const path of chapters) {
    await _checkStatus(_site + path, "GET", 200);
    await _checkRedirect(_site + path.slice(0, -1), _site + path);
  }

  for (const path of publishedAssets()) {
    await _checkStatus(_site + path, "HEAD", 200);
  }

  for (const path of images) {
    const response = await _checkStatus(_site + path, "HEAD", 200);
    if (!response.headers.get("content-type")?.startsWith("image/")) {
      _fail(_site + path, `not served as an image: ${response.headers.get("content-type")}`);
    }
  }

  for (const { path, target } of redirects) {
    await _checkRedirect(_site + path, target);
  }

  const downloads = redirects.filter(({ path }) => path.startsWith("/download/"));
  await _checkRedirect(`${_site}/download/254/?tmstv=1793477260`, downloads[0].target, { prefixOnly: true });
  for (const target of new Set(downloads.map((download) => download.target))) {
    const response = await fetch(target, { method: "HEAD" });
    if (!response.ok) {
      _fail(target, `download target answers ${response.status}`);
    }
  }

  for (const path of _hiddenPaths) {
    await _checkStatus(_site + path, "HEAD", 404);
  }

  const missing = await _checkStatus(`${_site}/this-page-does-not-exist/`, "GET", 404);
  if (!(await missing.text()).includes("vitepress")) {
    _fail(`${_site}/this-page-does-not-exist/`, "the 404 page is not the new site's");
  }
  await _checkStatus(`${_site}/sitemap.xml`, "GET", 200);
  await _checkStatus(`${_site}/robots.txt`, "GET", 200);

  await _checkStatus(`${_site}/.htaccess.wordpress`, "HEAD", 403);

  for (const url of [
    "https://os-robot.com/quick-start/",
    "http://www.os-robot.com/quick-start/",
    "http://os-robot.com/quick-start/",
  ]) {
    await _checkRedirect(url, `${_site}/quick-start/`);
  }
  await _checkRedirect("http://www.os-robot.com/", `${_site}/`);

  for (const failure of _failures) {
    console.log(failure);
  }
  console.log(_failures.length ? `${_failures.length} failed` : "All URLs OK");
  process.exitCode = _failures.length ? 1 : 0;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  await _run();
}
