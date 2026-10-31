// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import { publishedAssets, expandRedirects } from "../scripts/check-urls.mjs";

test("publishedAssets lists the build's scripts and styles as encoded URLs", () => {
  const assets = publishedAssets();
  assert.ok(assets.some((path) => /^\/assets\/style\.[^/]+\.css$/.test(path)));
  assert.ok(assets.some((path) => /^\/assets\/app\.[^/]+\.js$/.test(path)));
  // The search index's file name starts with @, and spaces or # would break a request.
  assert.ok(assets.every((path) => path === encodeURI(path) || path.includes("%40")));
  assert.ok(assets.some((path) => path.includes("%40localSearchIndex")));
});

test("expandRedirects tries an optional slash both ways", () => {
  assert.deepEqual(expandRedirects("RedirectMatch 301 ^/feed/?$ https://x.test/"), [
    { path: "/feed", target: "https://x.test/" },
    { path: "/feed/", target: "https://x.test/" },
  ]);
});

test("expandRedirects unescapes dots", () => {
  assert.deepEqual(expandRedirects("RedirectMatch 301 ^/a\\.xml$ https://x.test/b.xml"), [
    { path: "/a.xml", target: "https://x.test/b.xml" },
  ]);
});

test("expandRedirects gives an open-ended prefix a sample path", () => {
  assert.deepEqual(expandRedirects("RedirectMatch 301 ^/category/ https://x.test/"), [
    { path: "/category/uncategorized/", target: "https://x.test/" },
  ]);
});

test("expandRedirects ignores comments, 404 rules and the capture-group slash rule", () => {
  const htaccess = [
    "# RedirectMatch 301 ^/old$ https://x.test/",
    "RedirectMatch 404 ^/(wp-admin)",
    "RedirectMatch 301 ^/(a|b)$ https://x.test/$1/",
  ].join("\r\n");
  assert.deepEqual(expandRedirects(htaccess), []);
});
