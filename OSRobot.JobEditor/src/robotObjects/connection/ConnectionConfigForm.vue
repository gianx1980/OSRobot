<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <div class="q-pa-md">
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("general") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row q-mb-sm">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.waitSeconds"
              :label="$t('waitSecondsBeforeNextObj')"
              lazy-rules
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm" v-if="_sourceIsTask">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.runMode"
              :options="_runModes"
              :label="_$t('runNextObject')"
              dense
              map-options
              emit-value
            />
          </div>
        </div>
        <div
          class="row q-mb-sm"
          v-if="
            _sourceIsTask &&
            _propsRef.modelValue.runMode === 'OnceWithAllResults'
          "
        >
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.collectedResultRule"
              :options="_collectedResultRules"
              :label="_$t('collectedResultIsSuccessfulWhen')"
              :hint="_$t('runOnceWithAllResultsHint')"
              dense
              map-options
              emit-value
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.enabled"
              :label="$t('enabled')"
              left-label
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <ExecuteConditions
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
      conditionType="executeConditions"
    />

    <ExecuteConditions
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
      conditionType="dontExecuteConditions"
    />
  </div>
</template>
<script setup>
import { ref, computed, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import ExecuteConditions from "src/components/ExecuteConditions.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _formData = ref(_props);

// Connections saved before these settings existed don't have them: show the server's defaults.
if (!_propsRef.value.modelValue.runMode)
  _propsRef.value.modelValue.runMode = "ForEachResult";
if (!_propsRef.value.modelValue.collectedResultRule)
  _propsRef.value.modelValue.collectedResultRule = "AllSucceeded";

// An event always produces exactly one result, so how to run on several results only matters for tasks.
const _sourceIsTask = computed(() => {
  const source = _propsRef.value.containingFolderItems?.find(
    (t) => String(t.id) === String(_propsRef.value.modelValue.source)
  );
  return source?.type === "task";
});

const _runModes = [
  { label: _$t("runForEachResult"), value: "ForEachResult" },
  { label: _$t("runOnceWithAllResults"), value: "OnceWithAllResults" },
];

const _collectedResultRules = [
  { label: _$t("allIterationsSucceeded"), value: "AllSucceeded" },
  { label: _$t("anyIterationSucceeded"), value: "AnySucceeded" },
];
</script>
