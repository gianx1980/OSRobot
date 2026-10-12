<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("fileLoader") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row q-mb-md">
          <div class="col-9">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.filePath"
              :label="_$t('filePath')"
              :hint="_$t('filePathTextOrExcelHint')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnFileBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="filePath"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="filePath"
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.autodetectSeparator"
              :label="_$t('autodetectSeparator')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.separator"
              :label="_$t('separator')"
              :hint="_$t('separatorHint')"
              :disable="_propsRef.modelValue.autodetectSeparator"
              lazy-rules
              dense
              maxlength="10"
              :rules="[
                (val) =>
                  _propsRef.modelValue.autodetectSeparator ||
                  !!val ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.autodetectEncoding"
              :label="_$t('autodetectEncoding')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-select
              v-model="_propsRef.modelValue.encoding"
              :options="_encodingOptions"
              :label="_$t('encoding')"
              :hint="_$t('encodingHint')"
              :disable="_propsRef.modelValue.autodetectEncoding"
              lazy-rules
              dense
              map-options
              emit-value
              :rules="[
                (val) =>
                  _propsRef.modelValue.autodetectEncoding ||
                  !!val ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.sheetName"
              :label="_$t('sheetName')"
              :hint="_$t('sheetNameExcelHint')"
              dense
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="sheetName"
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.tableName"
              :label="_$t('destinationTable')"
              :hint="_$t('destinationTableHint')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="tableName"
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-select
              v-model="_propsRef.modelValue.ifTableExists"
              :options="_ifTableExistsOptions"
              :label="_$t('ifTableExists')"
              dense
              map-options
              emit-value
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.firstRowContainsColumnNames"
              :label="_$t('firstRowHasColumnNames')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.autodetectColumnTypes"
              :label="_$t('autodetectColumnTypes')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.culture"
              :label="_$t('culture')"
              :hint="_$t('cultureHint')"
              dense
              maxlength="20"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-11">
            <q-input
              type="number"
              filled
              v-model.number="_propsRef.modelValue.commandTimeout"
              :label="_$t('commandTimeoutSeconds')"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 2147483647) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '2147483647']),
              ]"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <SqlServerConnectionConfigForm
      v-model="_propsRef.modelValue"
      showDatabaseField="true"
      :testConnectionButtonLabel="_$t('testConnection')"
      @attemptedConnection="_attemptedConnection"
    />
    <PluginIterationConfigForm
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
    />
  </div>
</template>
<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import SqlServerConnectionConfigForm from "src/components/SqlServerConnectionConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

// Tasks saved before these settings existed don't have them: show the server's defaults.
if (_propsRef.value.modelValue.autodetectSeparator === undefined)
  _propsRef.value.modelValue.autodetectSeparator = true;
if (_propsRef.value.modelValue.autodetectEncoding === undefined)
  _propsRef.value.modelValue.autodetectEncoding = true;

// Values are .NET encoding names (Encoding.GetEncoding).
const _encodingOptions = [
  { label: "UTF-8", value: "utf-8" },
  { label: "Windows-1252 (Western Europe)", value: "windows-1252" },
  { label: "Windows-1250 (Central Europe)", value: "windows-1250" },
  { label: "Windows-1251 (Cyrillic)", value: "windows-1251" },
  { label: "Windows-1253 (Greek)", value: "windows-1253" },
  { label: "ISO-8859-1 (Latin 1)", value: "iso-8859-1" },
  { label: "ISO-8859-15 (Latin 9, with €)", value: "iso-8859-15" },
  { label: "UTF-16 LE", value: "utf-16" },
  { label: "UTF-16 BE", value: "utf-16BE" },
];

const _ifTableExistsOptions = [
  { label: _$t("fail"), value: "Fail" },
  { label: _$t("appendTheRows"), value: "Append" },
  { label: _$t("dropAndRecreateTheTable"), value: "DropAndRecreate" },
];

async function _attemptedConnection(eventArgs) {
  const message = eventArgs.result
    ? _$t("connectionSucceeded")
    : _$t("cantConnectToSqlServer");

  _$q.dialog({
    title: _$t("osRobot"),
    message: message,
    cancel: false,
    persistent: true,
  });
}
</script>
