<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("program") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.programPath"
              :label="_$t('programPath')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnFileBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="programPath"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="programPath"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.parameters"
              :label="_$t('parameters')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="parameters"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.workingFolder"
              :label="_$t('workingFolder')"
              lazy-rules
              dense
            />
          </div>
          <div class="col-3">
            <BtnFolderBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="workingFolder"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="workingFolder"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
</template>
<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import BtnFolderBrowser from "src/components/BtnFolderBrowser.vue";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;
</script>
