<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("configuration") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row">
          <div class="col-6">
            <q-input
              filled
              v-model.number="_formData.modelValue.host"
              :label="_$t('host')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.timeout"
              :label="_$t('timeoutMs')"
              type="number"
              min="0"
              max="99999"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 99999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '99999']),
              ]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model.number="_formData.modelValue.attempts"
              :label="_$t('attempts')"
              type="number"
              min="1"
              max="999"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 1 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['1', '999']),
              ]"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);
const _formData = ref(_props);

const _i18n = useI18n();
const _$t = _i18n.t;
</script>
