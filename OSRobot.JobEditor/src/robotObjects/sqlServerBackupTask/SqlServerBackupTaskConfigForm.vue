<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <SqlServerConnectionConfigForm
      v-model="_propsRef.modelValue"
      @attemptedConnection="_attemptedConnection"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("databases") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.backupType"
              :options="_backupTypes"
              :label="_$t('backupType')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.databasesToBackup"
              :options="_databasesToBackup"
              :label="_$t('databasesToBackup')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-table
              :columns="_databaseColumnsDef"
              :rows="_databaseList"
              :visible-columns="_databaseColumnVisibility"
              :no-data-label="_$t('thereAreNoItemsToShow')"
              selection="multiple"
              v-model:selected="_propsRef.modelValue.selectedDatabases"
              @selection="_databaseTableRowSelection"
              row-key="id"
              dense
            >
            </q-table>
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("destination") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.destinationPath"
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
              modelValueKey="destinationPath"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="destinationPath"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.fileNameTemplate"
              :label="_$t('fileNameTemplate')"
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
              modelValueKey="fileNameTemplate"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.overwriteIfExists"
              :label="_$t('overwriteFileIfExists')"
              left-label
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("other") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.verifyBackup"
              :label="_$t('verifyBackup')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.performChecksum"
              :label="_$t('performChecksum')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.continueOnError"
              :label="_$t('continueBackupIfChecksumErrorOccur')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.useCompression"
              :options="_compression"
              :label="_$t('compression')"
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
import SqlServerConnectionConfigForm from "src/components/SqlServerConnectionConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import ClientUtils from "src/infrastructure/server/ClientUtils.js";
import { useAppStore } from "src/stores/appStore.js";
import BtnFolderBrowser from "src/components/BtnFolderBrowser.vue";

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

// Copy path command management
const _backupTypes = [
  { label: _$t("full"), value: "Full" },
  { label: _$t("transactionLog"), value: "TransactionLog" },
];

const _databasesToBackup = [
  { label: _$t("allDatabases"), value: "AllDatabases" },
  { label: _$t("allUserDatabases"), value: "AllUserDatabases" },
  { label: _$t("selectedDatabases"), value: "SelectedDatabases" },
];

const _compression = [
  { label: _$t("useServerDefault"), value: "UseServerDefault" },
  { label: _$t("compressBackup"), value: "CompressBackup" },
  { label: _$t("doNotCompressBackup"), value: "DoNotCompressBackup" },
];

const _databaseColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "name",
    label: _$t("database"),
    align: "left",
    field: "name",
  },
];

const _databaseColumnVisibility = ref(["name"]);

const _databaseList = ref(_propsRef.value.modelValue.databaseList);

async function _attemptedConnection(eventArgs) {
  if (!eventArgs.result) {
    _$q.dialog({
      title: _$t("osRobot"),
      message: _$t("cantConnectToSqlServer"),
      cancel: false,
      persistent: true,
    });

    const emptyList = [];
    _databaseList.value = emptyList;
    _propsRef.value.modelValue.databaseList = emptyList;
    return;
  }

  const connectionInfo = {
    server: _propsRef.value.modelValue.server,
    username: _propsRef.value.modelValue.username,
    password: _propsRef.value.modelValue.password,
    connectionStringOptions: _propsRef.value.modelValue.connectionStringOptions,
  };

  try {
    const utils = new ClientUtils(_user.token, _user.refreshToken);
    const result = await utils.sqlServerGetDatabases(connectionInfo);

    _databaseList.value = result.responseObject;
    _propsRef.value.modelValue.databaseList = result.responseObject;
  } catch (e) {
    console.error(e);
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredDuringTheOperation"),
      position: "top",
    });
  }
}

function _databaseTableRowSelection(ev) {
  console.log(ev);
}
</script>
