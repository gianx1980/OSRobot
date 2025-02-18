<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />

    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("sourceAndDestination") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.source"
              :label="_$t('sourceArchive')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnFileBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="source"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="source"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.destination"
              :label="_$t('destinationFolder')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnFolderBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="destination"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="destination"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.compressionLevel"
              :options="_fileExistsOptions"
              :label="_$t('ifDestinationFileExists')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
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
import { useAppStore } from "src/stores/appStore.js";
import BtnFolderBrowser from "src/components/BtnFolderBrowser.vue";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _fileExistsOptions = [
  { label: _$t("overwrite"), value: "Overwrite" },
  { label: _$t("createWithUniqueNames"), value: "CreateWithUniqueNames" },
  { label: _$t("fail"), value: "Fail" },
];
</script>
