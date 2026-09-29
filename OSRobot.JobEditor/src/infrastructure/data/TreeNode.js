// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

export default class TreeNode {
  constructor(id, label, icon, header, workspaceFolder, children) {
    this.id = id;
    this.label = label;
    this.icon = icon;
    this.header = header;
    this.workspaceFolder = workspaceFolder;
    this.children = children || [];
  }
}
