// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import {
  buildSidebar,
  firstParagraph,
  githubSlug,
  rewriteLinks,
  slugFor,
  stripChapterNavigation,
} from "../scripts/sync-manual.mjs";

const _readmePath = new URL("../../docs/user-manual/README.md", import.meta.url);

test("slugFor drops the chapter number only", () => {
  assert.equal(slugFor("09-events.md"), "events");
  assert.equal(slugFor("appendix-a-examples.md"), "appendix-a-examples");
  assert.equal(slugFor("README.md"), "");
});

test("rewriteLinks maps chapter links and keeps anchors", () => {
  assert.equal(rewriteLinks("[x](09-events.md#webhook)"), "[x](/manual/events/#webhook)");
  assert.equal(rewriteLinks("[x](09-events.md)"), "[x](/manual/events/)");
});

test("rewriteLinks maps the manual's own README to the manual's first page", () => {
  assert.equal(rewriteLinks("[x](README.md)"), "[x](/manual/)");
});

test("rewriteLinks sends repo files to GitHub", () => {
  assert.equal(
    rewriteLinks("[d](../../OSRobot.Server/DEPLOYMENT.md#a)"),
    "[d](https://github.com/gianx1980/OSRobot/blob/master/OSRobot.Server/DEPLOYMENT.md#a)",
  );
});

test("rewriteLinks leaves external links and in-page anchors alone", () => {
  assert.equal(rewriteLinks("[p](https://pandoc.org/)"), "[p](https://pandoc.org/)");
  assert.equal(rewriteLinks("[a](#later)"), "[a](#later)");
});

test("githubSlug matches the anchors GitHub generates", () => {
  assert.equal(githubSlug("10.1 File tasks"), "101-file-tasks");
  assert.equal(githubSlug("Letting other machines call the webhooks"), "letting-other-machines-call-the-webhooks");
  assert.equal(githubSlug("A. Worked `examples`!"), "a-worked-examples");
});

test("firstParagraph skips headings and removes Markdown formatting", () => {
  const markdown = "# Title\n\n## Sub\n\nUse **bold** and `code` with a [link](x.md)\nacross lines.\n\nNext.";
  assert.equal(firstParagraph(markdown), "Use bold and code with a link across lines.");
});

test("firstParagraph skips chapter navigation and lines that introduce something", () => {
  assert.equal(firstParagraph("# T\n\nPrevious: [a](a.md) · Next: [b](b.md)\n\nReal text."), "Real text.");
  assert.equal(firstParagraph("# T\n\nNext: [b](b.md)\n\nReal text."), "Real text.");
  assert.equal(firstParagraph("# T\n\nFrom the source code, run:\n\nReal text."), "Real text.");
});

test("firstParagraph falls back to the title when there is no prose", () => {
  assert.equal(firstParagraph("# 13. Troubleshooting\n\n- item"), "13. Troubleshooting");
});

test("stripChapterNavigation removes the manual's own navigation line", () => {
  const markdown = "# T\n\nPrevious: [a](a.md) · Next: [b](b.md)\n\nText.\r\n\r\nNext: [b](b.md)\r\n";
  assert.equal(stripChapterNavigation(markdown).includes("Next:"), false);
  assert.equal(stripChapterNavigation(markdown).includes("Text."), true);
});

test("stripChapterNavigation leaves prose that mentions next steps alone", () => {
  const markdown = "Next: restart the server, then check the log.";
  assert.equal(stripChapterNavigation(markdown), markdown);
});

test("firstParagraph handles Windows line endings", () => {
  assert.equal(firstParagraph("# Title\r\n\r\nFirst.\r\n\r\nSecond."), "First.");
});

test("firstParagraph skips tables, lists and code", () => {
  assert.equal(firstParagraph("# T\n\n| a |\n|---|\n\n- item\n\n```\ncode\n```\n\nText."), "Text.");
});

test("buildSidebar keeps README order and includes the appendices", () => {
  const items = buildSidebar(readFileSync(_readmePath, "utf8"));
  assert.equal(items[0].link, "/manual/introduction/");
  assert.equal(items.at(-1).link, "/manual/appendix-b-quick-reference/");
  assert.equal(items.length, 15);
});

test("buildSidebar leaves out links to repo documents", () => {
  const items = buildSidebar(readFileSync(_readmePath, "utf8"));
  assert.ok(items.every((item) => item.link.startsWith("/manual/")));
});
