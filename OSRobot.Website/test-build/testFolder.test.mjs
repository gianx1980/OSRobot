// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Checks the copy of the site built for /_site-test/ (npm run build:test). It's kept apart
// from test/ because the normal build doesn't produce this output.

import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

const _distDir = fileURLToPath(new URL("../.vitepress/dist-test/", import.meta.url));

function _read(path) {
  return readFileSync(join(_distDir, path), "utf8");
}

test("every page tells search engines not to index it", () => {
  for (const path of ["index.html", "quick-start/index.html", "manual/logs/index.html", "404.html"]) {
    assert.match(_read(path), /<meta name="robots" content="noindex/, path);
  }
});

test("the canonical link still points at the real site", () => {
  assert.match(_read("quick-start/index.html"), /rel="canonical" href="https:\/\/www\.os-robot\.com\/quick-start\/"/);
});

test("internal links, images, footer and favicon stay inside the test folder", () => {
  const home = _read("index.html");
  assert.match(home, /href="\/_site-test\/downloads\/"/);
  assert.match(home, /href="\/_site-test\/privacy-terms-of-use\/"/);
  assert.match(home, /rel="icon" href="\/_site-test\/favicon\.jpeg"/);
  assert.match(_read("quick-start/index.html"), /src="\/_site-test\/wp-content\/uploads\/2025\/04\/Save\.png"/);
});

test("the images exist inside the test folder", () => {
  assert.ok(existsSync(join(_distDir, "wp-content/uploads/2025/04/Save.png")));
});

test("the test folder gets its own .htaccess, not the site's redirects", () => {
  const htaccess = _read(".htaccess");
  assert.match(htaccess, /^RewriteEngine Off$/m);
  assert.match(htaccess, /^ErrorDocument 404 \/_site-test\/404\.html$/m);
  assert.match(htaccess, /X-Robots-Tag "noindex, nofollow"/);
  assert.doesNotMatch(htaccess, /RedirectMatch/);
});

test("no sitemap is published for the test copy", () => {
  assert.equal(existsSync(join(_distDir, "sitemap.xml")), false);
  assert.equal(existsSync(join(_distDir, "robots.txt")), false);
});
