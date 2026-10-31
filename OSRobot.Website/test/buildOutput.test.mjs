// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { existsDist, readDist } from "./helpers.mjs";
import { slugFor } from "../scripts/sync-manual.mjs";

const _manualDir = fileURLToPath(new URL("../../docs/user-manual/", import.meta.url));

function _description(html) {
  return html.match(/<meta name="description" content="([^"]+)"/)[1];
}

test("home page keeps its title and canonical URL", () => {
  const html = readDist("index.html");
  assert.match(html, /<title>OS-Robot<\/title>/);
  assert.match(html, /rel="canonical" href="https:\/\/www\.os-robot\.com\/"/);
});

test("home page keeps its copy and download link", () => {
  const html = readDist("index.html");
  assert.match(html, /free, open-source, and cross-platform/);
  assert.match(html, /href="\/downloads\/"/);
});

test("no third-party scripts are loaded", () => {
  assert.doesNotMatch(readDist("index.html"), /<script[^>]+src="https?:/);
});

test("the project README is not published as a page", () => {
  assert.doesNotMatch(readDist("sitemap.xml"), /README/);
});

test("every manual chapter is built with its own description", () => {
  // The 404 page has no description of its own, so it shows the site's default.
  const siteDescription = _description(readDist("404.html"));
  const chapters = readdirSync(_manualDir).filter((file) => file.endsWith(".md") && file !== "README.md");
  assert.equal(chapters.length, 15);
  for (const file of chapters) {
    const description = _description(readDist(`manual/${slugFor(file)}/index.html`));
    assert.notEqual(description, siteDescription, file);
    assert.ok(description.length <= 155, file);
  }
});

test("no manual description is navigation or an unfinished sentence", () => {
  const chapters = readdirSync(_manualDir).filter((file) => file.endsWith(".md") && file !== "README.md");
  for (const file of chapters) {
    const description = _description(readDist(`manual/${slugFor(file)}/index.html`));
    assert.doesNotMatch(description, /^(Previous|Next):|:$/, file);
  }
});

test("manual pages show the theme's pager, not a second navigation line", () => {
  assert.doesNotMatch(readDist("manual/troubleshooting/index.html"), /<p>(Previous|Next): /);
});

test("manual pages link to the privacy page, because the theme hides the footer there", () => {
  const sidebar = readDist("manual/logs/index.html").match(/<aside class="VPSidebar[\s\S]*?<\/aside>/)[0];
  assert.match(sidebar, /href="\/privacy-terms-of-use\/"/);
});

test("manual chapters have canonical URLs with a trailing slash", () => {
  assert.match(
    readDist("manual/logs/index.html"),
    /rel="canonical" href="https:\/\/www\.os-robot\.com\/manual\/logs\/"/,
  );
});

test("literal placeholders survive in the manual", () => {
  assert.match(readDist("manual/logs/index.html"), /osrobot-&lt;date&gt;\.log/);
  assert.match(readDist("manual/dynamic-data/index.html"), /\{object\[/);
});

test("headings keep the GitHub-style anchors the manual's own links use", () => {
  const html = readDist("manual/tasks/index.html");
  assert.match(html, /href="#101-file-tasks"/);
  assert.match(html, /id="101-file-tasks"/);
});

test("every in-page and cross-chapter anchor in the manual has a target", () => {
  const slugs = ["", ...readdirSync(_manualDir).filter((f) => f.endsWith(".md") && f !== "README.md").map(slugFor)];
  const pagePath = (slug) => `manual/${slug ? `${slug}/` : ""}index.html`;
  const idsOf = (html) => new Set([...html.matchAll(/\sid="([^"]+)"/g)].map((m) => m[1]));
  const broken = [];

  for (const slug of slugs) {
    const html = readDist(pagePath(slug));
    for (const [, target, anchor] of html.matchAll(/href="(\/manual\/[\w-]*\/?)?#([^"]+)"/g)) {
      const targetSlug = target ? target.replace(/^\/manual\//, "").replace(/\/$/, "") : slug;
      if (!idsOf(readDist(pagePath(targetSlug))).has(anchor)) {
        broken.push(`${slug}: ${target ?? ""}#${anchor}`);
      }
    }
  }
  // Skip link and other theme anchors point at ids the theme adds in the browser.
  assert.deepEqual(broken.filter((link) => !link.endsWith("#VPContent")), []);
});

test("the manual's first page is published at /manual/", () => {
  assert.match(readDist("manual/index.html"), /OS-Robot User Manual/);
  assert.match(readDist("sitemap.xml"), /<loc>https:\/\/www\.os-robot\.com\/manual\/<\/loc>/);
});

for (const [path, title, phrase] of [
  ["installation", "Installation – OS-Robot", "Docker installation"],
  ["quick-start", "Quick start – OS-Robot", "Iterations and task instances"],
  ["privacy-terms-of-use", "Privacy &amp; terms of use – OS-Robot", "GPL-3.0"],
]) {
  test(`${path} keeps title, canonical and content`, () => {
    const html = readDist(`${path}/index.html`);
    assert.ok(html.includes(`<title>${title}</title>`));
    assert.ok(html.includes(`rel="canonical" href="https://www.os-robot.com/${path}/"`));
    assert.ok(html.includes(phrase));
  });
}

test("quick start images exist at their old paths", () => {
  const html = readDist("quick-start/index.html");
  const images = [...html.matchAll(/src="(\/wp-content\/uploads\/[^"]+)"/g)].map((m) => m[1]);
  assert.equal(new Set(images).size, 14);
  for (const image of images) {
    assert.ok(existsDist(image), image);
  }
});

test("quick start shows its syntax placeholders as text", () => {
  const html = readDist("quick-start/index.html");
  for (const name of ["objectID", "fieldName", "rowIndex", "subFieldName"]) {
    assert.ok(html.includes(`<code>&lt;${name}&gt;</code>`), name);
  }
});

test("the moved pages have descriptions of their own", () => {
  const siteDescription = _description(readDist("404.html"));
  for (const path of ["installation", "quick-start", "privacy-terms-of-use"]) {
    assert.notEqual(_description(readDist(`${path}/index.html`)), siteDescription, path);
  }
});

test("the installation page names the .NET 10 runtime, not .NET 8", () => {
  const html = readDist("installation/index.html");
  assert.match(html, /ASP\.NET 10/);
  assert.doesNotMatch(html, /ASP\.NET 8|dotnet\/8\.0|aspnet:8\.0/);
});

test("sitemap and robots point at www", () => {
  assert.match(readDist("sitemap.xml"), /<loc>https:\/\/www\.os-robot\.com\/<\/loc>/);
  assert.match(readDist("robots.txt"), /Sitemap: https:\/\/www\.os-robot\.com\/sitemap\.xml/);
});
