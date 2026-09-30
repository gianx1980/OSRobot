// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

export default class Folder {
  constructor(id, label, icon, header, content, children) {
    this.id = id;
    this.label = label;
    this.icon = icon;
    this.header = header;
    this.content = content || [];
    this.children = children || [];
  }
}
