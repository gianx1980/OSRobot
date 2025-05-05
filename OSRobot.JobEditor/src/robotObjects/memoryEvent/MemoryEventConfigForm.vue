<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("threshold") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.triggerIf"
              val="TriggerIfUsageAboveThreshold"
              :label="_$t('triggerIfUsageIsAboveThreshold')"
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.threshold"
              :label="_$t('thresholdPercentage')"
              type="number"
              min="0"
              max="100"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.triggerIf !==
                'TriggerIfUsageAboveThreshold'
              "
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 100) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '100']),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.triggerIf"
              val="TriggerIfAverageUsageAboveThreshold"
              :label="_$t('triggerIfAverageUsageIsAboveThreshold')"
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.thresholdLastXMin"
              :label="_$t('thresholdPercentage')"
              type="number"
              min="0"
              max="100"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.triggerIf !==
                'TriggerIfAverageUsageAboveThreshold'
              "
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 100) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '100']),
              ]"
            />
          </div>
          <div class="col-2 q-ml-xs text-center">{{ _$t("inTheLast") }}</div>
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.avgIntervalMinutes"
              :label="_$t('minutes')"
              type="number"
              min="0"
              max="999"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.triggerIf !==
                'TriggerIfAverageUsageAboveThreshold'
              "
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '999']),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.triggerIfPassedXMinFromLastTrigger"
              :label="_$t('triggerIfMinutesPassedSinceTheLastTrigger')"
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.minutesFromLastTrigger"
              :label="_$t('minutes')"
              type="number"
              min="0"
              max="999"
              lazy-rules
              dense
              :disable="
                !_formData.modelValue.triggerIfPassedXMinFromLastTrigger
              "
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '999']),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
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

// Handle "TriggerIf" property
if (!_formData.value.modelValue.hasOwnProperty("triggerIf")) {
  _formData.value.modelValue.triggerIf = "TriggerIfUsageAboveThreshold";
}

watch(
  () => _formData.value.modelValue.triggerIf,
  (newValue) => {
    switch (newValue) {
      case "TriggerIfUsageAboveThreshold":
        _formData.value.modelValue.triggerIfUsageIsAboveThreshold = true;
        _formData.value.modelValue.triggerIfAvgUsageIsAboveThresholdLastXMin = false;
        break;

      case "TriggerIfAverageUsageAboveThreshold":
        _formData.value.modelValue.triggerIfUsageIsAboveThreshold = false;
        _formData.value.modelValue.triggerIfAvgUsageIsAboveThresholdLastXMin = true;
        break;
    }
  }
);

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
