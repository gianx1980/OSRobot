// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Live connection to the server's notifications hub (NotifyUsersTask). Started by MainLayout
// while a user is logged in; notifications sent while it is disconnected are lost by design.

import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import {
  isSessionOver,
  nextRetryDelayMs,
} from "src/infrastructure/server/NotificationHubPolicy.js";
import { useAppStore } from "src/stores/appStore.js";

let _connection = null;
let _authFailed = false;
let _startTimer = null;

function _hubUrl() {
  return AppConfig.apiUrl.replace(/\/api\/?$/, "") + "/hubs/notifications";
}

// Called on every connect and reconnect: a tab left open for days still gets a valid token.
async function _accessToken() {
  const user = useAppStore().getLoggedUser();
  if (user === null) {
    _authFailed = true;
    throw new Error("No logged user.");
  }

  try {
    const service = new ServiceBase(user.token, user.refreshToken);
    return await service.ensureFreshToken();
  } catch (e) {
    // Only a refused renewal ends the session (stop quietly: the regular API calls send the user
    // to login). Network errors are left to the retry policy.
    if (isSessionOver(e)) _authFailed = true;
    throw e;
  }
}

function _nextRetryDelay(retryContext) {
  return _authFailed ? null : nextRetryDelayMs(retryContext.previousRetryCount);
}

async function _tryStart() {
  _startTimer = null;
  if (_connection === null || _authFailed) return;

  try {
    await _connection.start();
  } catch (e) {
    // Automatic reconnect only covers connections that were once up: retry the first start here.
    console.warn("Notifications hub: connection failed.", e);
    if (_connection !== null && !_authFailed)
      _startTimer = setTimeout(_tryStart, nextRetryDelayMs(Infinity));
  }
}

async function start(onNotification) {
  if (_connection !== null) return;

  _authFailed = false;
  _connection = new HubConnectionBuilder()
    .withUrl(_hubUrl(), {
      accessTokenFactory: _accessToken,
      // The token travels in the URL: no cookies needed, which keeps CORS simple.
      withCredentials: false,
    })
    .withAutomaticReconnect({ nextRetryDelayInMilliseconds: _nextRetryDelay })
    .configureLogging(LogLevel.Warning)
    .build();

  _connection.on("notification", onNotification);
  await _tryStart();
}

async function stop() {
  if (_startTimer !== null) {
    clearTimeout(_startTimer);
    _startTimer = null;
  }

  const connection = _connection;
  _connection = null;
  if (connection !== null) await connection.stop();
}

export default { start, stop };
