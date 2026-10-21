// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Reconnection decisions for NotificationHub.js, kept free of Vue/Quasar imports so they can be
// unit-tested with `npm test`.

// Relative (not "src/...") so Node can resolve it in unit tests.
import ServiceException from "./ServiceException.js";

const _retryDelaysMs = [0, 2000, 10000];
const _steadyRetryDelayMs = 30000;

// True only when the server refused to renew the token. A network or server error (server
// restarting, laptop waking up) is transient: the connection must keep retrying, or a tab left
// idle long enough for its token to need renewal would stop receiving notifications for good.
export function isSessionOver(error) {
  if (error instanceof ServiceException) return true;

  const status = error?.response?.status;
  return status === 401 || status === 403;
}

// Unlike SignalR's default (4 attempts, then give up), keep trying every 30 seconds, so a server
// restart that takes longer than a minute is still picked up.
export function nextRetryDelayMs(previousRetryCount) {
  return previousRetryCount < _retryDelaysMs.length
    ? _retryDelaysMs[previousRetryCount]
    : _steadyRetryDelayMs;
}
