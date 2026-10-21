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
        <div class="text-h6">{{ _$t("configuration") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row no-wrap">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.title"
              :label="_$t('title')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-auto">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="title"
            />
          </div>
        </div>
        <div class="row no-wrap">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.message"
              :label="_$t('message')"
              type="textarea"
              dense
            />
          </div>
          <div class="col-auto">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="message"
            />
          </div>
        </div>
        <div class="row q-mt-md">
          <div class="col-3">
            <q-select
              v-model="_propsRef.modelValue.level"
              :options="_levelOptions"
              :label="_$t('level')"
              dense
              map-options
              emit-value
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
    />
  </div>
</template>
<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _i18n = useI18n();
const _$t = _i18n.t;

// Values match UserNotificationLevel on the server.
const _levelOptions = [
  { label: _$t("levelInfo"), value: "Info" },
  { label: _$t("levelSuccess"), value: "Success" },
  { label: _$t("levelWarning"), value: "Warning" },
  { label: _$t("levelError"), value: "Error" },
];
</script>
