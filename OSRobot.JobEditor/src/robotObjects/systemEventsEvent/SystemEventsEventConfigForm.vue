<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("eventSelection") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventDisplaySettingsChanged"
              :label="_$t('displaySettingsChanged')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventInstalledFontsChanged"
              :label="_$t('installedFontsChanged')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventPaletteChanged"
              :label="_$t('paletteChanged')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventPowerModeChanged"
              :label="_$t('powerModeChanged')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventSessionEnded"
              :label="_$t('sessionEnded')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventSessionSwitch"
              :label="_$t('sessionSwitch')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventTimeChanged"
              :label="_$t('timeChanged')"
              dense
            />
          </div>
        </div>
        <div class="row row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_formData.modelValue.eventUserPreferenceChanged"
              :label="_$t('userPreferenceChanged')"
              dense
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
