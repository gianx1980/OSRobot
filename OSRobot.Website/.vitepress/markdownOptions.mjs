// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { githubSlug } from "../scripts/sync-manual.mjs";

export const markdownOptions = {
  anchor: { slugify: githubSlug },
  // The attribute syntax would swallow a trailing {object[ID].Field} and turn it into an HTML
  // attribute, removing it from the page while it still reads correctly on GitHub.
  attrs: { disable: true },
};
