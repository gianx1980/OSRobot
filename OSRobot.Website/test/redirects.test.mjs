// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import { join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { existsDist, readDist } from "./helpers.mjs";

const _distDir = fileURLToPath(new URL("../.vitepress/dist/", import.meta.url));

function _filesUnder(dir) {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) =>
    entry.isDirectory() ? _filesUnder(join(dir, entry.name)) : [join(dir, entry.name)],
  );
}

const _base = "https://github.com/gianx1980/OSRobot/releases/download";
const _latestZip = `${_base}/v1.2.0/os-robot_v1_2_0.zip`;
const _retiredDownloadIds = [249, 243, 235, 230, 193, 156, 150, 82, 62, 52];

const _rules = readFileSync(new URL("../public/.htaccess", import.meta.url), "utf8")
  .split(/\r?\n/)
  .map((line) => line.trim().replace(/\s+/g, " "))
  .filter((line) => line && !line.startsWith("#"));

test("the latest version's old download link still reaches its zip", () => {
  assert.ok(_rules.includes(`RedirectMatch 301 ^/download/254/?$ ${_latestZip}`));
});

test("download links of retired versions go to the Downloads page", () => {
  for (const id of _retiredDownloadIds) {
    assert.ok(_rules.includes(`RedirectMatch 301 ^/download/${id}/?$ https://www.os-robot.com/downloads/`), String(id));
  }
});

test("no redirect sends visitors to a retired version's zip", () => {
  const zipRules = _rules.filter((rule) => rule.startsWith("RedirectMatch") && rule.endsWith(".zip"));
  assert.deepEqual(zipRules, [`RedirectMatch 301 ^/download/254/?$ ${_latestZip}`]);
});

test("retired WordPress URLs are covered", () => {
  for (const pattern of [
    "^/no-access/?$",
    "^/feed/?$",
    "^/comments/feed/?$",
    "^/category/",
    "^/wp-sitemap\\.xml$",
    "^/wp-sitemap-posts-page-1\\.xml$",
    "^/wp-content/uploads/2025/12/os-robot_v1_0_7\\.zip$",
    "^/wp-content/uploads/2025/03/os-robot_v1_0_0\\.zip$",
  ]) {
    assert.ok(_rules.some((rule) => rule.startsWith(`RedirectMatch 301 ${pattern} `)), pattern);
  }
});

test("missing slashes go straight to https, avoiding Apache's http:// redirect", () => {
  const rule = _rules.find((line) => line.startsWith("RedirectMatch 301 ^/(downloads|"));
  assert.ok(rule);
  assert.ok(rule.endsWith(" https://www.os-robot.com/$1/"));
  for (const page of ["downloads", "installation", "quick-start", "privacy-terms-of-use", "manual"]) {
    assert.ok(rule.includes(page), page);
  }
});

test("dormant WordPress code is hidden", () => {
  const rule = _rules.find((line) => line.startsWith("RedirectMatch 404 "));
  assert.ok(rule);
  for (const part of ["wp-admin", "wp-login", "xmlrpc", "wp-includes", "plugins", "themes"]) {
    assert.ok(rule.includes(part), part);
  }
});

test("anything PHP-like is hidden, including the renamed index.php.off", () => {
  const rule = _rules.find((line) => line.startsWith("RedirectMatch 404 ") && line.includes("\\.ph(p|ar|tml)"));
  assert.ok(rule);
  assert.ok(rule.includes("(?i)"));
});

test("the wp-content folder itself is hidden, since nothing serves its index any more", () => {
  assert.ok(_rules.some((line) => line.startsWith("RedirectMatch 404 ") && line.includes("wp-content/?$")));
});

test("no 404 rule hides anything the site publishes", () => {
  const patterns = _rules
    .filter((line) => line.startsWith("RedirectMatch 404 "))
    .map((line) => line.split(" ")[2].replace("(?i)", ""))
    .map((pattern) => new RegExp(pattern, "i"));
  assert.equal(patterns.length, 2);

  for (const file of _filesUnder(_distDir)) {
    const url = `/${relative(_distDir, file).replaceAll("\\", "/")}`;
    assert.ok(!patterns.some((pattern) => pattern.test(url)), url);
  }
});

test("the 404 page is configured and .htaccess reaches the build", () => {
  assert.ok(_rules.includes("ErrorDocument 404 /404.html"));
  assert.ok(existsDist(".htaccess"));
});

test("the downloads page keeps its title and links straight to the latest zip", () => {
  const html = readDist("downloads/index.html");
  assert.ok(html.includes("<title>Downloads – OS-Robot</title>"));
  assert.ok(html.includes(_latestZip));
});

test("the downloads page offers only the latest version", () => {
  const html = readDist("downloads/index.html");
  const zips = new Set([...html.matchAll(/href="([^"]+\.zip)"/g)].map((match) => match[1]));
  assert.deepEqual([...zips], [_latestZip]);
  assert.doesNotMatch(html, /Old versions/);
});
