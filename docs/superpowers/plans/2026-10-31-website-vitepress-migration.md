# OS-Robot Website Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace WordPress at `https://www.os-robot.com` with a static VitePress site on the existing Aruba hosting, keeping every indexed URL working and adding the user manual.

**Architecture:** A VitePress project in `OSRobot.Website/` builds the current pages plus a manual generated from `docs/user-manual/` (which stays the only source of the manual). The build output is uploaded by FTP to the current Aruba web folder, and an `.htaccess` file maps every retired WordPress URL. Release zips live on GitHub Releases. DNS and email aren't touched.

**Tech Stack:** VitePress 1.6.4, Node 20, `node:test`, Apache `.htaccess` on Aruba hosting, GitHub Releases.

**Spec:** None. The decisions and the measured facts are recorded in the next sections.

**Who does what:** Steps marked **[You]** need your accounts or your approval to publish (Claude can't enter passwords or publish without you). Everything else is Claude's.

## Decisions

- **Hosting:** stay on Aruba for now. No DNS, name server or email record changes of any kind.
- **Publishing:** build locally and upload by FTP. Automated deployment is left out until manual uploads become a nuisance.
- **Zips:** use GitHub Releases. `dlm_uploads` answers 403 to direct requests, new releases already go to GitHub, and the zips stay out of the site's uploads.
- **Rollback:** WordPress stays on the server, switched off rather than deleted, until the new site has proved itself.
- **Later option:** moving to Cloudflare Pages stays possible. These URLs, pages and zips carry over, and only `.htaccess` would need converting.

## Global Constraints

- Canonical host stays `https://www.os-robot.com`.
- These URLs keep returning 200 with the same content: `/`, `/downloads/`, `/installation/`, `/quick-start/`, `/privacy-terms-of-use/`.
- Page titles stay as they are: `OS-Robot` on the home page, `<Page> – OS-Robot` elsewhere (en dash).
- Every redirect that replaces a WordPress URL is a permanent 301.
- Quick start images keep their exact `/wp-content/uploads/...` paths.
- No analytics, tracking cookies or third-party scripts.
- `docs/user-manual/` is not edited for the site: the pandoc printable copy and repo links must keep working.
- SPDX header on every `.mts`, `.mjs` and `.vue` file. Module-level helpers are prefixed `_`.
- **Nothing is committed, pushed or uploaded without your say-so.** Each "Checkpoint" stops for your review, and you decide whether it's committed.
- Pages move as they are: besides the runtime version fix in Task 3, rewrites wait until rankings have settled.

## Facts Measured On 2026-10-31

| Item | Value |
|---|---|
| Server | `aruba-proxy` in front of Apache. Tested on 2026-10-31: `.htaccess` is honored, including `RedirectMatch 301`, `RedirectMatch 404` and `ErrorDocument` with a real 404 status; `.htaccess` itself answers 403 |
| `www` and HTTPS | `os-robot.com` and `http://` answer 301 to `https://www` with the path preserved. This still works in a folder with `RewriteEngine Off`, and for static files, so it comes from Aruba's proxy or panel rather than WordPress |
| Trailing slash | Apache's automatic redirect goes to `http://www.../x/`, then the proxy sends it to `https://`, so it takes two hops unless `.htaccess` handles it |
| Email and DNS | Stay at Aruba, unchanged |
| Search Console | A `google-site-verification` TXT record exists, so it appears to be set up already |
| Content | 6 sitemap URLs, 0 posts, no SEO plugin, no meta descriptions, no analytics scripts or forms found on the home page |
| Runtime | `OSRobot.Server` targets `net10.0`; the live `/installation/` page still says ASP.NET 8 |
| GitHub assets present | v1.2.0, v1.1.0, v1.0.3, v1.0.2 |
| GitHub assets missing | v1.0.7, v1.0.6, v1.0.5, v1.0.4, v1.0.1, v1.0.0 have releases without zips; v1.0.8 has no release |
| WordPress zip oddities | v1.0.4 and v1.0.5 report the same size (42824 KB); three v1.0.1 files and two v1.0.0 files exist |
| Local tools | Node 20.15.1; `gh` is not installed |

### Download redirects

Base URL: `https://github.com/gianx1980/OSRobot/releases/download`

| WordPress URL | Target under base URL |
|---|---|
| `/download/254/` | `v1.2.0/os-robot_v1_2_0.zip` |
| `/download/249/` | `v1.1.0/os-robot_v1_1_0.zip` |
| `/download/243/` | `v1.0.8/os-robot_v1_0_8.zip` |
| `/download/235/` | `v1.0.7/os-robot_v1_0_7.zip` |
| `/download/230/` | `v1.0.6/os-robot_v1_0_6.zip` |
| `/download/193/` | `v1.0.5/os-robot_v1_0_5.zip` |
| `/download/156/` | `v1.0.4/os-robot_v1_0_4.zip` |
| `/download/150/` | `v1.0.3/os-robot_v1_0_3.zip` |
| `/download/82/` | `v1.0.2/os-robot_v1_0_2.zip` |
| `/download/62/` | `v1.0.1/os-robot_v1_0_1.zip` |
| `/download/52/` | `v1.0.0/os-robot_v1_0_0.zip` |

## Review Focus

1. **A visitor opens an old link without the trailing slash (`/quick-start`):** they should land on the page through a permanent redirect. Checked in Task 6.
2. **The `www` and HTTPS redirects stop working once WordPress is off:** `https://os-robot.com/quick-start/` and `http://www.os-robot.com/` must still reach `https://www`. Checked in Task 6.
3. **An old download link carries a query (`/download/254/?tmstv=1793477260`):** it should still reach the zip. Checked in Task 6.
4. **The manual contains text Vue or Markdown could swallow (`<date>`, `{object[ID].Field}`):** it should appear literally on the page. Checked in Task 2.
5. **Dormant WordPress code stays reachable while switched off:** `/wp-login.php`, `/xmlrpc.php`, `/wp-admin/` and PHP files under plugins must return 404. Checked in Task 6.

---

### Task 0: Baseline, backups and server test [You + Claude]

Do this first: if the server test fails, the plan changes before anything is built.

**Files:** none in the repo.

- [ ] **Step 1 [You]: Export the baseline from Search Console.** Confirm you can open the `os-robot.com` property. Export Pages and Queries for the last 12 months and save them outside the repo.
- [ ] **Step 2 [You]: Take backups.** Take a full backup of files and database from Aruba's panel, plus a WordPress XML export (Tools → Export).
- [ ] **Step 3 [You]: Find your FTP details and web folder in Aruba's panel,** and connect with an FTP client such as FileZilla. Turn on "show hidden files" so `.htaccess` is visible. Keep the password to yourself.
- [ ] **Step 4 [You]: Send Claude a copy of the root `.htaccess`,** after removing anything private. Claude identifies any rule that must survive, especially `www` or HTTPS rules.
- [ ] **Step 5 [You]: Check Aruba's panel for a page cache setting** and note how to purge it.
- [ ] **Step 6: Claude prepares a test folder `_site-test/`** containing `index.html` and an `.htaccess` with `ErrorDocument 404 /_site-test/missing.html`, a `RedirectMatch 301` rule and a `RedirectMatch 404` rule.
- [ ] **Step 7 [You]: Upload `_site-test/` to the web folder.**
- [ ] **Step 8: Verify the server test.** Check by request:
  - `/_site-test/` returns 200.
  - `/_site-test` returns 301 to the slash form.
  - The 301 rule returns 301, the 404 rule returns 404, and a missing path returns 404 with the custom page.
  - `https://os-robot.com/_site-test/` and `http://www.os-robot.com/_site-test/` reach `https://www`.
- [ ] **Step 9 [You]: Delete `_site-test/` from the server.**
- [ ] **Step 10: Add new URLs from the export to the coverage list.** Any URL with traffic that isn't in this plan gets a redirect in Task 4.

**If `.htaccess` isn't honored:** stop. Check whether the plan needs Windows hosting's `web.config` format instead, or come back to the Cloudflare option.

### Task 1: VitePress project skeleton

Work in a git worktree on a new branch `website` from `master` (use superpowers:using-git-worktrees), so the uncommitted `11_Dev` changes stay untouched.

**Files:**
- Create: `OSRobot.Website/package.json`, `OSRobot.Website/.gitignore`, `OSRobot.Website/README.md`
- Create: `OSRobot.Website/.vitepress/config.mts`
- Create: `OSRobot.Website/index.md`
- Create: `OSRobot.Website/public/favicon.jpeg`, `OSRobot.Website/public/robots.txt`
- Test: `OSRobot.Website/test/buildOutput.test.mjs`, `OSRobot.Website/test/helpers.mjs`
- Modify: `CLAUDE.md` (solution layout)

**Interfaces:**
- Produces: `npm run dev`, `npm run build` (output in `.vitepress/dist`), `npm test` (`node --test test/`, reads the build output); `readDist(path): string` and `existsDist(path): boolean` from `test/helpers.mjs`.

- [ ] **Step 1: Write the failing test `buildOutput.test.mjs`**

```js
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
test("sitemap and robots point at www", () => {
  assert.match(readDist("sitemap.xml"), /<loc>https:\/\/www\.os-robot\.com\/<\/loc>/);
  assert.match(readDist("robots.txt"), /Sitemap: https:\/\/www\.os-robot\.com\/sitemap\.xml/);
});
```

- [ ] **Step 2: Run it.** Run: `npm install && npm test`. Expected: FAIL because `.vitepress/dist` doesn't exist.
- [ ] **Step 3: Implement the skeleton.**
  - Pin `vitepress` to exactly `1.6.4`.
  - In `config.mts`, set: `lang: "en-US"`, `title: "OS-Robot"`, `titleTemplate: ":title – OS-Robot"`, `description` (one sentence, used by pages without their own), `sitemap.hostname: "https://www.os-robot.com"`, `themeConfig.search.provider: "local"`, `cleanUrls: false`.
  - Add `transformPageData` or `transformHead` to emit one canonical link per page: `index.md` → `/`, `x/index.md` → `/x/`.
  - Set the nav to Downloads, Installation, Quick start, Manual, plus a GitHub social link to `https://github.com/gianx1980/OSRobot`.
  - Set the footer to a Privacy and terms link and `Copyright © 2025-2026 | OS-Robot.com`.
  - In `index.md`, use `layout: home` and `titleTemplate: false`, with the exact home copy from the live site and a Download button to `/downloads/`. Add a one-sentence `description`.
  - Create `public/favicon.jpeg` from `/wp-content/uploads/2025/03/WhatsApp-Image-2025-03-05-at-23.09.26-1-300x300.jpeg`.
  - Add `dist`, `cache` and `manual` entries to `.gitignore`.
  - `README.md`: how to run, build and upload the site, including the hidden `.htaccess` and "build from `master`".
  - Add `OSRobot.Website` to the layout section of `CLAUDE.md`.
- [ ] **Step 4: Verify.** Run: `npm run build && npm test`. Expected: PASS. Check `npm audit` output and report anything high.
- [ ] **Step 5: Checkpoint.** Show the home page in a local preview and wait for your review.

### Task 2: Generate the manual from `docs/user-manual`

**Files:**
- Create: `OSRobot.Website/scripts/sync-manual.mjs`
- Modify: `OSRobot.Website/package.json` (`predev`, `prebuild`), `OSRobot.Website/.vitepress/config.mts` (sidebar)
- Test: `OSRobot.Website/test/syncManual.test.mjs`, `OSRobot.Website/test/buildOutput.test.mjs`

**Interfaces:**
- Produces (from `sync-manual.mjs`):
  - `slugFor(fileName: string): string`
  - `rewriteLinks(markdown: string): string`
  - `firstParagraph(markdown: string): string`
  - `buildSidebar(readmeMarkdown: string): {text: string, link: string}[]`
  - `syncManual(sourceDir: string, targetDir: string): void`

- [ ] **Step 1: Write the failing tests**

```js
test("slugFor drops the chapter number only", () => {
  assert.equal(slugFor("09-events.md"), "events");
  assert.equal(slugFor("appendix-a-examples.md"), "appendix-a-examples");
  assert.equal(slugFor("README.md"), "");
});
test("rewriteLinks maps chapter links and keeps anchors", () => {
  assert.equal(rewriteLinks("[x](09-events.md#webhook)"), "[x](/manual/events/#webhook)");
});
test("rewriteLinks sends repo files to GitHub", () => {
  assert.equal(
    rewriteLinks("[d](../../OSRobot.Server/DEPLOYMENT.md#a)"),
    "[d](https://github.com/gianx1980/OSRobot/blob/master/OSRobot.Server/DEPLOYMENT.md#a)",
  );
});
test("rewriteLinks leaves external links alone", () => {
  assert.equal(rewriteLinks("[p](https://pandoc.org/)"), "[p](https://pandoc.org/)");
});
test("buildSidebar keeps README order and includes the appendices", () => {
  const items = buildSidebar(readFileSync("../docs/user-manual/README.md", "utf8"));
  assert.equal(items[0].link, "/manual/introduction/");
  assert.equal(items.at(-1).link, "/manual/appendix-b-quick-reference/");
});
```

Add to `buildOutput.test.mjs`:

```js
test("every manual chapter is built with its own description", () => {
  for (const file of readdirSync("../docs/user-manual")) {
    if (!file.endsWith(".md") || file === "README.md") continue;
    const html = readDist(`manual/${slugFor(file)}/index.html`);
    const description = html.match(/<meta name="description" content="([^"]+)"/)[1];
    assert.notEqual(description, SITE_DESCRIPTION); // the value of `description` in config.mts
  }
});
test("literal placeholders survive in the manual", () => {
  assert.match(readDist("manual/logs/index.html"), /osrobot-&lt;date&gt;\.log/);
  assert.match(readDist("manual/dynamic-data/index.html"), /\{object\[/);
});
```

- [ ] **Step 2: Run them.** Run: `npm test`. Expected: FAIL because `sync-manual.mjs` doesn't exist.
- [ ] **Step 3: Implement `sync-manual.mjs`.**
  - `syncManual` deletes `targetDir`, then writes `README.md` to `manual/index.md` and each chapter to `manual/<slug>/index.md`.
  - Add frontmatter `description` from `firstParagraph`, cut to 155 characters on a word boundary and written with `JSON.stringify`.
  - Run it directly when invoked as a script, from `predev` and `prebuild`.
  - Feed `buildSidebar` into the `/manual/` sidebar in `config.mts`.
- [ ] **Step 4: Verify.** Run: `npm run build && npm test`. Expected: PASS, with no dead-link errors. Fix any real dead link in the script, not by turning on `ignoreDeadLinks`.
- [ ] **Step 5: Confirm the source is untouched.** Run: `git status docs/user-manual`. Expected: no changes.
- [ ] **Step 6: Checkpoint.** Show the manual in the preview and wait for your review.

### Task 3: Move the existing pages

Downloads for this task: the 14 Quick start images from `www.os-robot.com`, about 460 KB in total, plus the favicon.

**Files:**
- Create: `OSRobot.Website/installation/index.md`, `quick-start/index.md`, `privacy-terms-of-use/index.md`
- Create: `OSRobot.Website/public/wp-content/uploads/2025/04/*`, `public/wp-content/uploads/2025/06/*`
- Test: `OSRobot.Website/test/buildOutput.test.mjs`

- [ ] **Step 1: Write the failing test**

```js
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
  for (const image of images) assert.ok(existsDist(image));
});
```

- [ ] **Step 2: Run it.** Run: `npm run build && npm test`. Expected: FAIL because the pages don't exist.
- [ ] **Step 3: Convert the pages.** Pull `content.rendered` from `/wp-json/wp/v2/pages` and convert to Markdown using a throwaway script outside the repo.
  - Keep the wording, heading levels, alt text and code blocks.
  - Download each image to the same path under `public/`.
  - Reference images by their absolute `/wp-content/...` URL. Files in `public/` aren't renamed by the build, and the test above confirms it.
  - Add a one-sentence `description` to each page.
- [ ] **Step 4: Correct the runtime version.** Run `git show v1.2.0:OSRobot.Server/OSRobot.Server.csproj`. If it targets `net10.0`, change only the ASP.NET 8 prerequisite, its download link and the Docker `aspnet:8.0-alpine` line to .NET 10 on `/installation/`.
- [ ] **Step 5: Update the privacy page.** Add one sentence saying that downloads are served by GitHub under its own policy. **[You]** review this wording: it's your legal text.
- [ ] **Step 6: Verify.** Run: `npm run build && npm test`. Expected: PASS. Compare each page side by side with the live one in the browser.
- [ ] **Step 7: Checkpoint.** Wait for your review of the pages.

### Task 4: Downloads page and `.htaccess`

Downloads for this task: 7 zips from `www.os-robot.com`, about 288 MB in total. Fetching them through `/download/ID/` may add one to each Download Monitor counter.

**Files:**
- Create: `OSRobot.Website/downloads/index.md`
- Create: `OSRobot.Website/public/.htaccess`
- Test: `OSRobot.Website/test/redirects.test.mjs`

**Interfaces:**
- Produces: `public/.htaccess`, read by Task 5's checker.

- [ ] **Step 1: Download the missing zips and compare them.** Fetch `/download/243/`, `235`, `230`, `193`, `156`, `62` and `52` to a new scratch folder, recording each response's filename. Compute SHA-256 for each. Report whether v1.0.4 and v1.0.5 are the same file, and which v1.0.1 and v1.0.0 files are actually served.
- [ ] **Step 2 [You]: Publish the missing assets on GitHub.** Attach the zips to the existing releases v1.0.7, v1.0.6, v1.0.5, v1.0.4, v1.0.1 and v1.0.0. Create release v1.0.8 and attach its zip, first checking `git tag` for the tag. File names must match the redirect table. If v1.0.4 and v1.0.5 turn out to be identical, decide whether to publish that file for both or drop one.
- [ ] **Step 3: Write the failing test `redirects.test.mjs`**

```js
test("all 11 download IDs redirect permanently to their zip, with or without slash", () => {
  for (const [id, target] of DOWNLOADS) {
    assert.ok(rules.includes(`RedirectMatch 301 ^/download/${id}/?$ ${BASE}/${target}`));
  }
});
test("retired WordPress URLs are covered", () => {
  for (const pattern of ["^/no-access/?$", "^/feed/?$", "^/comments/feed/?$", "^/category/",
                         "^/wp-sitemap\\.xml$", "^/wp-sitemap-posts-page-1\\.xml$"]) {
    assert.ok(rules.some((r) => r.startsWith("RedirectMatch 301 ") && r.includes(pattern)), pattern);
  }
});
test("dormant WordPress code is hidden", () => {
  assert.ok(rules.some((r) => r.startsWith("RedirectMatch 404 ")
    && ["wp-admin", "wp-login", "xmlrpc", "wp-includes", "plugins", "themes"].every((p) => r.includes(p))));
});
test("the 404 page is configured and .htaccess reaches the build", () => {
  assert.ok(rules.includes("ErrorDocument 404 /404.html"));
  assert.ok(existsDist(".htaccess"));
});
test("downloads page links straight to GitHub", () => {
  const html = readDist("downloads/index.html");
  for (const [, target] of DOWNLOADS) assert.ok(html.includes(`${BASE}/${target}`));
});
```

`DOWNLOADS` is the table in Facts, and `BASE` is its base URL. `rules` is the non-empty, non-comment lines of `public/.htaccess`, with whitespace collapsed to single spaces.

- [ ] **Step 4: Run it.** Run: `npm run build && npm test`. Expected: FAIL.
- [ ] **Step 5: Implement.**
  - `downloads/index.md`: the same disclaimer, "Latest version" and "Old versions" sections and release dates as the live page. Link straight to GitHub, because VitePress's client router would treat `/download/ID/` as a missing internal page.
  - `.htaccess`:
    - `ErrorDocument 404 /404.html`.
    - One `RedirectMatch 301` adding the slash on `https://www` for the real folders (`/downloads`, `/installation`, `/quick-start`, `/privacy-terms-of-use`, `/manual` and `/manual/<slug>`). Apache's own slash redirect points at `http://` behind Aruba's proxy, which adds a needless extra hop (measured in Task 0).
    - One `RedirectMatch 301` per download.
    - `/no-access/`, `/feed/`, `/comments/feed/` and `/category/...` → `https://www.os-robot.com/`.
    - Both sitemap paths → `https://www.os-robot.com/sitemap.xml`.
    - `/wp-content/uploads/2025/12/os-robot_v1_0_7.zip` and `/wp-content/uploads/2025/03/os-robot_v1_0_0.zip` → `https://www.os-robot.com/downloads/`. Files in `dlm_uploads` already answer 403 and keep doing so.
    - One `RedirectMatch 404` covering `/wp-admin`, `/wp-login.php`, `/xmlrpc.php`, `/wp-includes/`, `/wp-content/plugins/`, `/wp-content/themes/` and root `wp-*.php` files.
    - Any rule that must survive from Task 0, Step 4, and anything from Task 0, Step 10.
    - **No WordPress rewrite block.**
- [ ] **Step 6: Verify.** Run: `npm run build && npm test`. Expected: PASS.
- [ ] **Step 7: Checkpoint.** Wait for your review.

### Task 5: URL checker

**Files:**
- Create: `OSRobot.Website/scripts/check-urls.mjs`

**Interfaces:**
- Consumes: `public/.htaccess`.
- Produces: `node scripts/check-urls.mjs`, which checks `https://www.os-robot.com`, prints one line per failure and exits non-zero if there are any.

- [ ] **Step 1: Implement the checks,** using `fetch` with `redirect: "manual"`:
  - The 5 pages return 200 and contain the expected `<title>`.
  - The 4 folder pages without a trailing slash return 301 directly to the `https://www` slash form, with no `http://` hop.
  - `/manual/` and every chapter return 200.
  - All 14 images return 200 with an `image/` content type.
  - Every download URL, with and without its slash, returns 301 to its target.
  - `/download/254/?tmstv=1793477260` returns 301 to a URL starting with the v1.2.0 zip.
  - Every other `RedirectMatch 301` returns 301 to its target.
  - Every download target answers 200 after following redirects (HEAD only).
  - `/wp-login.php`, `/xmlrpc.php`, `/wp-admin/` and `/wp-content/plugins/download-monitor/download-monitor.php` return 404.
  - `/this-page-does-not-exist/` returns a real 404 containing the new site's page.
  - `/sitemap.xml` and `/robots.txt` return 200.
  - `https://os-robot.com/quick-start/` and `http://www.os-robot.com/quick-start/` return 301 to `https://www.os-robot.com/quick-start/`.
- [ ] **Step 2: Prove it detects failure.** Run it while WordPress is still live. Expected: non-zero exit, listing at least the download redirects and `/manual/`. Those failures are correct at this stage.

### Task 6: Go live [You + Claude]

Pick a quiet time. The switch takes minutes, with a short period where pages may be mixed.

**Files:** none in the repo.

- [ ] **Step 1 [You]: Sign off on the local preview,** including a phone-sized viewport, and decide on committing and merging `website` into `master`. The published manual comes from `master`, so features still on `11_Dev` appear when that branch merges and the site is rebuilt.
- [ ] **Step 2: Build from `master`.** Run: `npm ci && npm run build && npm test`. Expected: PASS.
- [ ] **Step 3 [You]: Take a fresh backup** of the files and database.
- [ ] **Step 4 [You]: Save a listing of the web folder, then rename on the server.** The listing shows whether `robots.txt` or a favicon already exists, which the rollback would otherwise delete rather than restore. In the web folder, rename: `index.php` → `index.php.off` and `.htaccess` → `.htaccess.wordpress`. WordPress's files stay in place but stop answering.
- [ ] **Step 5 [You]: Upload the contents of `.vitepress/dist` to the web folder,** including the hidden `.htaccess`. Delete nothing. The images merge into the existing `wp-content/uploads` folders.
- [ ] **Step 6 [You]: Purge Aruba's page cache,** if Task 0, Step 5 found one.
- [ ] **Step 7: Run the checker.** Run: `node scripts/check-urls.mjs`. Expected: exit 0. If it can't be made to pass in the same session, roll back.
- [ ] **Step 8: Look through the live site in the browser,** including the home page, navigation, search, one manual chapter and one download.
- [ ] **Step 9 [You]: Submit `https://www.os-robot.com/sitemap.xml` in Search Console,** remove the old `wp-sitemap.xml` entry if listed, and use URL Inspection on the 5 main pages.
- [ ] **Step 10 [You]: Test email once** in each direction. Nothing here should affect it, but it's a quick reassurance.

**Rollback:** delete the uploaded `.htaccess`; rename `.htaccess.wordpress` and `index.php.off` back; and remove the new `index.html`, `404.html`, `sitemap.xml`, `robots.txt`, `favicon.jpeg`, `hashmap.json`, `vp-icons.css`, `assets/`, `downloads/`, `installation/`, `quick-start/`, `privacy-terms-of-use/` and `manual/`. Real folders override WordPress's URLs, so they must go. Purge the cache. If that doesn't restore the site, restore the Step 3 backup.

### Task 7: Monitor, then retire WordPress [You + Claude]

**Files:** none in the repo.

- [ ] **Step 1: Check after 1, 7 and 28 days.** Re-run the checker. **[You]** send the Pages and Queries exports, and Claude compares them with the Task 0 baseline, adding redirects for any new 404s.
- [ ] **Step 2: Follow up after 4 stable weeks.** Rewrite `/installation/` and `/quick-start/` to match the current manual as separate changes. They have known errors (for example `JobEnginConfig`, and mixed `os-robot`/`osrobot` paths).
- [ ] **Step 3 [You]: Retire WordPress after at least 30 days with no rollback.** Keep the backups off the server. Remove `wp-admin/`, `wp-includes/`, `wp-content/plugins/`, `wp-content/themes/`, the `wp-*.php` files (including `wp-config.php`), `xmlrpc.php`, `index.php.off`, `.htaccess.wordpress` and `wp-content/uploads/dlm_uploads/`. Delete the WordPress database last. This is permanent, so Claude lists exactly what remains first and you do the deleting.
- [ ] **Step 4: Remove the `RedirectMatch 404` line only if you want to,** and re-run the checker. Keeping it is harmless and cuts down on bot noise.
