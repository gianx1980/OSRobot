<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <q-item
    :clickable="_state !== 'unsupported'"
    :disable="_state === 'unsupported'"
    @click="_onClick"
  >
    <q-item-section avatar><q-icon :name="_icon" /></q-item-section>
    <q-item-section>
      <q-item-label>
        {{ _$t("desktopNotifications") }}: {{ _stateLabel }}
      </q-item-label>
      <q-item-label v-if="_caption" caption>{{ _caption }}</q-item-label>
    </q-item-section>
  </q-item>
</template>
<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import {
  getDesktopState,
  requestOrToggleDesktop,
} from "src/infrastructure/NotificationPresenter.js";

const _i18n = useI18n();
const _$t = _i18n.t;

// The menu content mounts each time the menu opens, so this reflects the current permission.
const _state = ref(getDesktopState());

const _icon = computed(() =>
  _state.value === "granted-on" ? "notifications_active" : "notifications_off"
);

const _stateLabel = computed(() => {
  if (_state.value === "granted-on") return _$t("desktopNotificationsOn");
  if (_state.value === "denied") return _$t("desktopNotificationsBlocked");
  return _$t("desktopNotificationsOff");
});

const _caption = computed(() => {
  if (_state.value === "unsupported")
    return _$t("desktopNotificationsUnsupported");
  if (_state.value === "denied") return _$t("desktopNotificationsBlockedHint");
  return null;
});

async function _onClick() {
  await requestOrToggleDesktop();
  _state.value = getDesktopState();
}
</script>
