<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("failuresToReport") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row">
          <div class="col">
            <q-select
              v-model="_formData.modelValue.scope"
              :options="_scopeOptions"
              :label="_$t('watch')"
              dense
              map-options
              emit-value
            />
          </div>
        </div>
        <div class="row q-mt-md">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.reportOnlyUnhandled"
              :label="_$t('reportOnlyUnhandledFailures')"
              dense
            />
            <div class="text-caption text-grey-8 q-mt-xs">
              {{ _$t("reportOnlyUnhandledFailuresHint") }}
            </div>
          </div>
        </div>
        <div class="row q-mt-md">
          <div class="col-4">
            <q-input
              filled
              v-model.number="_formData.modelValue.groupForSeconds"
              :label="_$t('groupFailuresForSeconds')"
              type="number"
              min="0"
              max="3600"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 3600) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '3600']),
              ]"
            />
          </div>
        </div>
        <div class="text-caption text-grey-8">
          {{ _$t("groupFailuresHint") }}
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);
const _formData = ref(_props);

const _i18n = useI18n();
const _$t = _i18n.t;

// Values of TaskFailureEventScope on the server.
const _scopeOptions = [
  {
    label: _$t("scopeThisFolderAndSubfolders"),
    value: "ThisFolderAndSubfolders",
  },
  { label: _$t("scopeThisFolder"), value: "ThisFolder" },
  { label: _$t("scopeAllFolders"), value: "AllFolders" },
];

watch(
  () => _formData.value.modelValue.name,
  (newName, oldName) => {
    const eventArgs = {
      nodeId: _formData.value.modelValue.id,
      newName: newName,
      oldName: oldName,
    };
    _emit("nodeNeedsUpdate", eventArgs);
  }
);
</script>
