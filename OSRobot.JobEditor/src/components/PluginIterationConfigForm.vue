<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

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
            :label="_$t('recordsetToIterate')"
            hint="{object[N].FieldName}"
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
              (val) =>
                _propsRef.modelValue.pluginIterationMode !==
                  'IterateObjectRecordset' ||
                _recordsetReferenceRegex.test(val ?? '') ||
                _$t('mustBeADynamicDataReference'),
            ]"
          />
        </div>
        <div class="col-1">
          <q-btn
            square
            class="q-ml-sm"
            size="xs"
            icon="bolt"
            color="primary"
            :disable="
              _propsRef.modelValue.pluginIterationMode !==
              'IterateObjectRecordset'
            "
            @click="_selectIterationObjectClick"
            ><q-tooltip>{{ _$t("selectARecordsetToIterate") }}</q-tooltip></q-btn
          >
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
import { useQuasar } from "quasar";
import DynamicDataBrowserDialog from "src/components/DynamicDataBrowserDialog.vue";

import { useI18n } from "vue-i18n";
const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["update:modelValue"]);
const _propsRef = ref(_props);

// The engine resolves the iteration object as one plain reference: no [CODE], no text around it.
const _recordsetReferenceRegex = /^\{object\[\d+\]\.\w+\}$/i;

function _selectIterationObjectClick() {
  // Every object of the folder except this task itself
  const containingFolderItems = (
    _propsRef.value.containingFolderItems ?? []
  ).filter(
    (t) => t.id !== _propsRef.value.modelValue.id && t.type !== "folder"
  );

  _$q
    .dialog({
      component: DynamicDataBrowserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
        containingFolderItems: containingFolderItems,
        recordsetsOnly: true,
      },
    })
    .onOk((ev) => {
      // Replace, don't append: the field holds exactly one reference.
      _propsRef.value.modelValue.iterationObject = `{object[${ev.objectSelected.id}].${ev.dynDataSelected.internalName}}`;
    });
}
</script>
