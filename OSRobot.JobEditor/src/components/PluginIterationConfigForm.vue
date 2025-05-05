<template>
  <q-card>
    <q-card-section>
      <div class="text-h6">{{ _$t("iterations") }}</div>
    </q-card-section>

    <q-card-section class="q-pt-none">
      <div class="row">
        <div class="col">
          <q-radio
            v-model="_propsRef.modelValue.pluginIterationMode"
            val="IterateDefaultRecordset"
            :label="_$t('iterateAsManyTimesAsDefaultRecordset')"
            dense
          />
        </div>
      </div>
      <div class="row q-mt-md">
        <div class="col">
          <q-radio
            v-model="_propsRef.modelValue.pluginIterationMode"
            val="IterateObjectRecordset"
            :label="_$t('iterateAsManyTimesAsThisRecordset')"
            dense
          />
        </div>
      </div>
      <div class="row q-mt-xs">
        <div class="col-4">
          <q-input
            filled
            v-model="_propsRef.modelValue.iterationObject"
            :label="_$t('objectIDFieldName')"
            lazy-rules
            dense
            :disable="
              _propsRef.modelValue.pluginIterationMode !==
              'IterateObjectRecordset'
            "
            :rules="[
              (val) =>
                (_propsRef.modelValue.pluginIterationMode ===
                  'IterateObjectRecordset' &&
                  !!val) ||
                _$t('thisFieldIsMandatory'),
            ]"
          />
        </div>
      </div>
      <div class="row">
        <div class="col">
          <q-radio
            v-model="_propsRef.modelValue.pluginIterationMode"
            val="IterateExactNumber"
            :label="_$t('iterateThisExactNumberOfTimes')"
            dense
          />
        </div>
      </div>
      <div class="row q-mt-xs">
        <div class="col-4">
          <q-input
            filled
            v-model.number="_propsRef.modelValue.iterationsCount"
            :label="_$t('iterationsNumber')"
            type="number"
            min="0"
            max="999999"
            lazy-rules
            dense
            :disable="
              _propsRef.modelValue.pluginIterationMode !== 'IterateExactNumber'
            "
            :rules="[
              (val) =>
                (_propsRef.modelValue.pluginIterationMode ===
                  'IterateExactNumber' &&
                  (val === 0 || !!val)) ||
                _$t('thisFieldIsMandatory'),
              (val) =>
                (val >= 0 && val <= 100) ||
                _$t('mustBeAValueBetweenXAndY', ['0', '999999']),
            ]"
          />
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const _i18n = useI18n();
const _$t = _i18n.t;

const _props = defineProps(["modelValue"]);
const _emit = defineEmits(["update:modelValue"]);
const _propsRef = ref(_props);
</script>
