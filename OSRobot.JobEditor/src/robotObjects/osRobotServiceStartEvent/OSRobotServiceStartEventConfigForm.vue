<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("serviceStartTime") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.triggerIf"
              val="TriggerIfStartsWithinNMinutes"
              :label="
                _$t(
                  'triggerIfServiceStartsWithinTheFollowingNumberOfMinutesFromComputerBoot'
                )
              "
              dense
            />
          </div>
        </div>
        <div class="row q-mt-xs">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.minutesWithin"
              :label="_$t('minutes')"
              type="number"
              min="1"
              max="9999"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.triggerIf !==
                'TriggerIfStartsWithinNMinutes'
              "
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 1 && val <= 9999) ||
                  _$t('mustBeAValueBetweenXAndY', ['1', '9999']),
              ]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.triggerIf"
              val="TriggerIfStartsAfterNMinutes"
              :label="
                _$t(
                  'triggerIfServiceStartsAfterTheFollowingNumberOfMinutesFromComputerBoot'
                )
              "
              dense
            />
          </div>
        </div>
        <div class="row q-mt-xs">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.minutesAfter"
              :label="_$t('minutes')"
              type="number"
              min="1"
              max="9999"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.triggerIf !==
                'TriggerIfStartsAfterNMinutes'
              "
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
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
  _formData.value.modelValue.triggerIf = "TriggerIfStartsWithinNMinutes";
}

watch(
  () => _formData.value.modelValue.triggerIf,
  (newValue) => {
    switch (newValue) {
      case "TriggerIfStartsWithinNMinutes":
        _formData.value.modelValue.minutesAfter = null;
        break;

      case "TriggerIfStartsAfterNMinutes":
        _formData.value.modelValue.minutesWithin = null;
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
