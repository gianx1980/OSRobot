// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import PendingNotifications from "../src/infrastructure/PendingNotifications.js";

test("keeps everything up to the limit, in arrival order", () => {
  const pending = new PendingNotifications(3);
  ["a", "b", "c"].forEach((title) => pending.push({ title }));

  const { items, droppedCount } = pending.drain();

  assert.deepEqual(
    items.map((n) => n.title),
    ["a", "b", "c"]
  );
  assert.equal(droppedCount, 0);
});

test("beyond the limit keeps only the most recent and counts the rest", () => {
  const pending = new PendingNotifications(5);
  for (let i = 1; i <= 576; i++) pending.push({ title: `n${i}` });

  const { items, droppedCount } = pending.drain();

  assert.deepEqual(
    items.map((n) => n.title),
    ["n572", "n573", "n574", "n575", "n576"]
  );
  assert.equal(droppedCount, 571);
});

test("drain empties the queue and resets the count", () => {
  const pending = new PendingNotifications(1);
  pending.push({ title: "a" });
  pending.push({ title: "b" });
  pending.drain();

  assert.deepEqual(pending.drain(), { items: [], droppedCount: 0 });
});
