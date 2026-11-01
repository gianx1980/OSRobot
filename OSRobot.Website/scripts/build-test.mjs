// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Builds a private copy of the site for /_site-test/ on the live server, to try it there
// before go-live. WordPress keeps serving everything else, so the copy needs its own
// .htaccess and must stay out of search results.

import { execFileSync } from "node:child_process";
import { rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { syncManual } from "./sync-manual.mjs";

const _folder = "/_site-test/";
const _websiteDir = fileURLToPath(new URL("..", import.meta.url));
const _distDir = join(_websiteDir, ".vitepress", "dist-test");

const _htaccess = `# Temporary copy of the site, for testing. Delete this folder after testing.
# Turns off the parent WordPress rewrite rules for this folder only.
RewriteEngine Off

DirectoryIndex index.html
ErrorDocument 404 ${_folder}404.html

<IfModule mod_headers.c>
Header set X-Robots-Tag "noindex, nofollow"
</IfModule>
`;

syncManual(join(_websiteDir, "..", "docs", "user-manual"), join(_websiteDir, "manual"));

execFileSync("npx", ["vitepress", "build"], {
  cwd: _websiteDir,
  shell: true,
  stdio: "inherit",
  env: { ...process.env, OSROBOT_TEST_BASE: _folder },
});

rmSync(join(_distDir, "robots.txt"), { force: true });
writeFileSync(join(_distDir, ".htaccess"), _htaccess);
