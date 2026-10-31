// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import { createMarkdownRenderer } from "vitepress";
import { markdownOptions } from "../.vitepress/markdownOptions.mjs";

test("a trailing dynamic data placeholder stays on the page", async () => {
  const md = await createMarkdownRenderer(".", markdownOptions);
  assert.match(md.render("Use the value {object[ID].Field}"), />Use the value \{object\[ID\]\.Field\}</);
});
