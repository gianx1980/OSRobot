// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

const _distDir = fileURLToPath(new URL("../.vitepress/dist/", import.meta.url));

export function readDist(path) {
  return readFileSync(join(_distDir, path), "utf8");
}

export function existsDist(path) {
  return existsSync(join(_distDir, path));
}
