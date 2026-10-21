// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Notifications waiting for the tab to become visible. Bounded: a job notifying every few minutes
// over a weekend must not pile up hundreds of toasts for when the user comes back.
export default class PendingNotifications {
  constructor(maxKept) {
    this._maxKept = maxKept;
    this._items = [];
    this._droppedCount = 0;
  }

  push(payload) {
    this._items.push(payload);
    if (this._items.length > this._maxKept) {
      this._items.shift();
      this._droppedCount++;
    }
  }

  // The most recent ones, oldest first, and how many older ones were dropped.
  drain() {
    const result = { items: this._items, droppedCount: this._droppedCount };
    this._items = [];
    this._droppedCount = 0;
    return result;
  }
}
