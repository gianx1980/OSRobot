<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <SqlServerConnectionConfigForm
      v-model="_propsRef.modelValue"
      showDatabaseField="true"
      :testConnectionButtonLabel="_$t('testConnection')"
      @attemptedConnection="_attemptedConnection"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("bulkCopy") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-11">
            <q-input
              type="number"
              filled
              v-model="_propsRef.modelValue.commandTimeout"
              :label="_$t('commandTimeoutSeconds')"
              lazy-rules
              dense
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val > 0 && val < 2147483647) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '2147483647']),
              ]"
            />
          </div>
          <div class="col-1"></div>
        </div>
        <div class="row">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.sourceRecordset"
              :label="_$t('sourceRecordset')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="sourceRecordset"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-11">
            <q-input
              filled
              v-model="_propsRef.modelValue.destinationTable"
              :label="_$t('destinationTable')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="destinationTable"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
</template>
<script setup>
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import SqlServerConnectionConfigForm from "src/components/SqlServerConnectionConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

async function _attemptedConnection(eventArgs) {
  const message = eventArgs.result
    ? _$t("connectionSucceeded")
    : _$t("cantConnectToSqlServer");

  _$q.dialog({
    title: _$t("osRobot"),
    message: message,
    cancel: false,
    persistent: true,
  });
}
</script>
