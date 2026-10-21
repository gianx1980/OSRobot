// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

// Shows NotifyUsersTask notifications: in-app when the tab is in front, as a system notification
// when it is in the background (if allowed and not muted), otherwise in-app when the user returns.
// Text is always shown as plain text: messages can carry job data.

import { Notify } from "quasar";
import PendingNotifications from "src/infrastructure/PendingNotifications.js";

const _mutedStorageKey = "OSRDesktopNotificationsMuted";
const _inAppTimeoutMs = 8000;
const _maxPendingShown = 5;

const _notifyTypes = {
  Info: "info",
  Success: "positive",
  Warning: "warning",
  Error: "negative",
};

const _pending = new PendingNotifications(_maxPendingShown);
let _listeningForVisibility = false;
let _translate = (key) => key;

// The summary of dropped notifications is translated: MainLayout passes vue-i18n's t().
export function initPresenter(translate) {
  _translate = translate;
}

// Browsers only offer notifications on HTTPS or localhost.
function _isSupported() {
  return "Notification" in window && window.isSecureContext;
}

function _isMuted() {
  try {
    return localStorage.getItem(_mutedStorageKey) === "true";
  } catch {
    return false;
  }
}

function _setMuted(muted) {
  try {
    localStorage.setItem(_mutedStorageKey, String(muted));
  } catch {
    // Storage unavailable (private mode): the choice just isn't remembered.
  }
}

function _showInApp(payload) {
  const isError = payload.level === "Error";
  Notify.create({
    type: _notifyTypes[payload.level] ?? "info",
    message: payload.title,
    caption: payload.message,
    html: false,
    position: "bottom-right",
    timeout: isError ? 0 : _inAppTimeoutMs,
    actions: isError ? [{ icon: "close", color: "white", round: true }] : [],
  });
}

function _showSystem(payload) {
  const notification = new Notification(payload.title, {
    body: payload.message,
  });
  notification.onclick = () => {
    window.focus();
    notification.close();
  };
}

// Notifications that arrived while the tab was hidden and couldn't be shown by the system:
// shown when the user comes back, so they don't time out unseen.
function _queueUntilVisible(payload) {
  _pending.push(payload);
  if (_listeningForVisibility) return;

  _listeningForVisibility = true;
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState !== "visible") return;

    const { items, droppedCount } = _pending.drain();
    if (droppedCount > 0)
      Notify.create({
        type: "info",
        message: _translate("notificationsMissed", { count: droppedCount }),
        html: false,
        position: "bottom-right",
        timeout: 0,
        actions: [{ icon: "close", color: "white", round: true }],
      });
    items.forEach(_showInApp);
  });
}

export function present(payload) {
  if (document.visibilityState === "visible") {
    _showInApp(payload);
    return;
  }

  if (getDesktopState() === "granted-on") {
    try {
      _showSystem(payload);
      return;
    } catch {
      // Some browsers (e.g. Chrome on Android) refuse the constructor: fall back to in-app.
    }
  }

  _queueUntilVisible(payload);
}

/** "unsupported" | "default" | "granted-on" | "granted-off" | "denied" */
export function getDesktopState() {
  if (!_isSupported()) return "unsupported";
  if (Notification.permission === "denied") return "denied";
  if (Notification.permission === "granted")
    return _isMuted() ? "granted-off" : "granted-on";
  return "default";
}

// Must run from a click: browsers only ask for permission after a user gesture.
export async function requestOrToggleDesktop() {
  const state = getDesktopState();
  if (state === "default") {
    if ((await Notification.requestPermission()) === "granted")
      _setMuted(false);
  } else if (state === "granted-on") {
    _setMuted(true);
  } else if (state === "granted-off") {
    _setMuted(false);
  }
}
