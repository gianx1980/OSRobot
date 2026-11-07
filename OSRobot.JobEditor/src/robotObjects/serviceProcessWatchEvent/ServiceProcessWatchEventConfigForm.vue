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
        <div class="text-h6">{{ _$t("watch") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <q-select
          v-model="_formData.modelValue.watchType"
          :options="_watchTypeOptions"
          :label="_$t('watch')"
          dense
          map-options
          emit-value
          class="q-mb-md"
        />
        <q-input
          filled
          dense
          v-model="_formData.modelValue.namePattern"
          :label="
            _isService ? _$t('watchedServiceNames') : _$t('watchedProcessNames')
          "
          maxlength="1000"
          lazy-rules
          :rules="[(val) => !!val?.trim() || _$t('thisFieldIsMandatory')]"
        />
        <div class="text-caption text-grey-8">
          {{
            _isService
              ? _$t("watchedServiceNamesHint")
              : _$t("watchedProcessNamesHint")
          }}
        </div>
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("triggerWhen") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <template v-if="_isService">
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onServiceStopped"
              :label="_$t('whenServiceStops')"
              dense
            />
          </div>
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onServiceStarted"
              :label="_$t('whenServiceStarts')"
              dense
            />
          </div>
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onServicePaused"
              :label="_$t('whenServiceIsPaused')"
              dense
            />
          </div>
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onAnyServiceStateChange"
              :label="_$t('whenServiceChangesState')"
              dense
            />
          </div>
        </template>
        <template v-else>
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onProcessStarted"
              :label="_$t('whenProcessStarts')"
              dense
            />
          </div>
          <div class="row q-mb-sm">
            <q-toggle
              v-model="_formData.modelValue.onProcessExited"
              :label="_$t('whenProcessExits')"
              dense
            />
          </div>
        </template>
        <div v-if="!_anyConditionSelected" class="text-caption text-negative">
          {{ _$t("noWatchConditionSelected") }}
        </div>
        <div class="row q-mt-md q-mb-xs">
          <div class="col">
            <label class="q-ml-xs">{{ _$t("checkEvery") }}</label>
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.checkIntervalSeconds"
              :label="_$t('seconds')"
              type="number"
              min="1"
              max="9999"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 1 && val <= 9999) ||
                  _$t('mustBeAValueBetweenXAndY', ['1', '9999']),
              ]"
            />
          </div>
        </div>
        <div class="text-caption text-grey-8">
          {{ _$t("serviceProcessWatchHint") }}
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);
const _formData = ref(_props);

const _i18n = useI18n();
const _$t = _i18n.t;

// Values of WatchType on the server.
const _watchTypeOptions = [
  { label: _$t("watchServices"), value: "Service" },
  { label: _$t("watchProcesses"), value: "Process" },
];

const _isService = computed(
  () => _formData.value.modelValue.watchType !== "Process"
);

const _anyConditionSelected = computed(() => {
  const config = _formData.value.modelValue;

  return _isService.value
    ? config.onServiceStopped ||
        config.onServiceStarted ||
        config.onServicePaused ||
        config.onAnyServiceStateChange
    : config.onProcessStarted || config.onProcessExited;
});
</script>
