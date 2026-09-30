<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 440px; max-width: 440px">
      <q-card-section class="row items-center q-gutter-sm">
        <q-icon name="smart_toy" size="32px" color="primary" />
        <div>
          <div class="text-h6">{{ _appTitle }}</div>
          <div class="text-caption text-grey-7">
            {{ _$t("version") }} {{ _serverVersion }}
          </div>
        </div>
      </q-card-section>

      <q-separator />

      <q-card-section class="q-gutter-sm">
        <div class="text-body2">{{ _$t("aboutDescription") }}</div>

        <div class="text-body2">
          {{ _$t("copyright") }} {{ _copyrightHolder }}
        </div>

        <div class="text-body2">
          {{ _$t("license") }}:
          <a :href="_licenseUrl" target="_blank" rel="noopener">{{
            _$t("gplV3")
          }}</a>
        </div>

        <div class="text-caption text-grey-8">
          {{ _$t("noWarrantyDisclaimer") }}
        </div>
      </q-card-section>

      <q-separator />

      <q-card-section class="q-gutter-xs">
        <div class="text-body2">
          <q-icon name="language" class="q-mr-xs" />
          <a :href="_websiteUrl" target="_blank" rel="noopener">{{
            _websiteUrl
          }}</a>
        </div>
        <div class="text-body2">
          <q-icon name="code" class="q-mr-xs" />
          <a :href="_sourceUrl" target="_blank" rel="noopener">{{
            _$t("sourceCode")
          }}</a>
        </div>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn flat :label="_$t('close')" color="primary" @click="onDialogOK" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _serverConfig = _appStore.getServerConfig();

const _appTitle = _serverConfig?.appTitle || "OSRobot";
const _serverVersion = _serverConfig?.serverVersion || "-";

const _copyrightHolder = "2025-2026 Gianluca Di Bucci (gianx1980)";
const _licenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html";
const _websiteUrl = "https://www.os-robot.com";
const _sourceUrl = "https://github.com/gianx1980/OSRobot";

defineEmits([...useDialogPluginComponent.emits]);

const { dialogRef, onDialogHide, onDialogOK } = useDialogPluginComponent();
</script>
