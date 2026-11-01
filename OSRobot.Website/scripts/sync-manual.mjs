// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Generates the site's manual pages from docs/user-manual, which stays the only source:
// those files also have to work on GitHub and in the pandoc printable copy, so they
// can't be written for the site. Everything site-specific happens here.

import { mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const _repoBlobUrl = "https://github.com/gianx1980/OSRobot/blob/master/";
const _maxDescriptionLength = 155;

function _manualLink(fileName, anchor = "") {
  const slug = slugFor(fileName);
  return `/manual/${slug ? `${slug}/` : ""}${anchor}`;
}

function _truncateOnWord(text, maxLength) {
  if (text.length <= maxLength) {
    return text;
  }
  return text.slice(0, text.lastIndexOf(" ", maxLength)).replace(/[\s,;:(]+$/, "");
}

export function slugFor(fileName) {
  if (fileName === "README.md") {
    return "";
  }
  return fileName.replace(/\.md$/, "").replace(/^\d+-/, "");
}

// The manual's links are written for GitHub's heading anchors, which differ from
// VitePress's: "10.1 File tasks" is #101-file-tasks on GitHub and #_10-1-file-tasks here.
export function githubSlug(heading) {
  return heading
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s_-]/gu, "")
    .replace(/\s/g, "-");
}

// On GitHub the chapters need their own Previous / Next line. On the site the theme already
// shows a pager, so the line would appear twice.
export function stripChapterNavigation(markdown) {
  return markdown
    .replace(/^(Previous|Next): \[[^\]]+\]\([^)]+\)( · Next: \[[^\]]+\]\([^)]+\))?[ \t]*\r?$/gm, "")
    .replace(/(\r?\n)+---\s*$/, "\n");
}

export function rewriteLinks(markdown) {
  return markdown.replace(/\]\(([^)\s]+)\)/g, (match, target) => {
    if (target.startsWith("../../")) {
      return `](${_repoBlobUrl}${target.slice("../../".length)})`;
    }
    const chapter = target.match(/^([\w-]+\.md)(#.*)?$/);
    return chapter ? `](${_manualLink(chapter[1], chapter[2])})` : match;
  });
}

export function firstParagraph(markdown) {
  const paragraph = markdown
    .split(/\r?\n\s*\r?\n/)
    .map((block) => block.trim())
    .find(
      (block) =>
        block && !/^(#|\||-|\*\s|>|```|\d+\.\s|Previous: \[|Next: \[)/.test(block) && !block.endsWith(":"),
    );
  const title = markdown.match(/^#\s+(.+)$/m)?.[1];

  return (paragraph ?? title ?? "")
    .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
    .replace(/[*`]/g, "")
    .replace(/\s+/g, " ");
}

export function buildSidebar(readmeMarkdown) {
  return [...readmeMarkdown.matchAll(/\[([^\]]+)\]\(([\w-]+\.md)\)/g)].map(([, text, fileName]) => ({
    text,
    link: _manualLink(fileName),
  }));
}

export function syncManual(sourceDir, targetDir) {
  rmSync(targetDir, { recursive: true, force: true });

  for (const fileName of readdirSync(sourceDir).filter((file) => file.endsWith(".md"))) {
    const markdown = readFileSync(join(sourceDir, fileName), "utf8");
    const description = _truncateOnWord(firstParagraph(markdown), _maxDescriptionLength);
    // JSON string syntax is valid YAML, so any quote or colon in the text stays safe.
    const page = `---\ndescription: ${JSON.stringify(description)}\n---\n\n${rewriteLinks(stripChapterNavigation(markdown))}`;

    const pageDir = join(targetDir, slugFor(fileName));
    mkdirSync(pageDir, { recursive: true });
    writeFileSync(join(pageDir, "index.md"), page);
  }
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const websiteDir = dirname(dirname(fileURLToPath(import.meta.url)));
  syncManual(join(websiteDir, "..", "docs", "user-manual"), join(websiteDir, "manual"));
}
