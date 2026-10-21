// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import { test } from "node:test";
import assert from "node:assert/strict";
import {
  isSessionOver,
  nextRetryDelayMs,
} from "../src/infrastructure/server/NotificationHubPolicy.js";
import ServiceException from "../src/infrastructure/server/ServiceException.js";

test("a network error while renewing the token does not end the session", () => {
  // axios error with no response: server restarting, laptop waking up, network blip.
  const networkError = new Error("Network Error");
  assert.equal(isSessionOver(networkError), false);
});

test("a server error while renewing the token does not end the session", () => {
  const serverError = Object.assign(new Error("Request failed"), {
    response: { status: 500 },
  });
  assert.equal(isSessionOver(serverError), false);
});

test("a refused refresh token ends the session", () => {
  const refused = Object.assign(new Error("Request failed"), {
    response: { status: 401 },
  });
  assert.equal(isSessionOver(refused), true);
});

test("a refresh answered with an error code ends the session", () => {
  assert.equal(isSessionOver(new ServiceException(-32766, null)), true);
});

test("retries quickly at first, then every 30 seconds forever", () => {
  assert.deepEqual(
    [0, 1, 2, 3, 50].map(nextRetryDelayMs),
    [0, 2000, 10000, 30000, 30000]
  );
});
