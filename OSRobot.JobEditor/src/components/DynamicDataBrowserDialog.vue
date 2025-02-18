<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 700px; max-width: 80vw">
      <q-card-section>
        <div class="text-h6">{{ _$t("objects") }}</div>
      </q-card-section>
      <q-card-section>
        <q-table
          style="height: 230px; table-layout: fixed"
          :columns="_objectTableColumnsDef"
          :rows="_propsRef.containingFolderItems"
          selection="single"
          v-model:selected="_objectSelected"
          :visible-columns="_objectTableColumnsVisibility"
          :no-data-label="_$t('thereAreNoObjectsToShow')"
          row-key="id"
          dense
          @selection="_objectTableRowSelection"
          @row-click="_objectTableRowClick"
        ></q-table>
      </q-card-section>

      <q-card-section>
        <div class="text-h6">{{ _$t("dynamicData") }}</div>
      </q-card-section>
      <q-card-section>
        <q-table
          style="height: 230px; table-layout: fixed"
          :columns="_dynDataTableColumnsDef"
          :rows="_dynDataSamplesList"
          selection="single"
          v-model:selected="_dynDataSelected"
          :visible-columns="_dynDataTableColumnsVisibility"
          :no-data-label="_$t('thereIsNoDynamicDataToShow')"
          row-key="internalName"
          dense
          @row-click="_dynDataTableRowClick"
        ></q-table>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('cancel')" @click="onDialogCancel" />
        <q-btn
          color="primary"
          :label="_$t('select')"
          @click="onOKClick"
          :disable="
            _objectSelected.length === 0 || _dynDataSelected.length === 0
          "
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import Robot from "src/infrastructure/server/Robot.js";
import Utility from "src/infrastructure/Utility.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _router = useRouter();

const _props = defineProps(["containingFolderItems"]);
const _propsRef = ref(_props);

const _objectSelected = ref([]);

const _dynDataSelected = ref([]);

defineEmits([
  // REQUIRED; need to specify some events that your
  // component will emit through useDialogPluginComponent()
  ...useDialogPluginComponent.emits,
]);

const { dialogRef, onDialogHide, onDialogOK, onDialogCancel } =
  useDialogPluginComponent();
// dialogRef      - Vue ref to be applied to QDialog
// onDialogHide   - Function to be used as handler for @hide on QDialog
// onDialogOK     - Function to call to settle dialog with "ok" outcome
//                    example: onDialogOK() - no payload
//                    example: onDialogOK({ /*...*/ }) - with payload
// onDialogCancel - Function to call to settle dialog with "cancel" outcome

// this is part of our example (so not required)
function onOKClick() {
  const selection = {
    objectSelected: _objectSelected.value[0],
    dynDataSelected: _dynDataSelected.value[0],
  };

  onDialogOK(selection);
}

// Objects table
const _objectTableColumnsDef = [
  {
    name: "id",
    label: _$t("id"),
    align: "left",
    field: "id",
  },
  {
    name: "name",
    label: _$t("name"),
    align: "left",
    field: "label",
  },
  {
    name: "type",
    label: _$t("type"),
    align: "left",
    field: "type",
  },
  {
    name: "pluginId",
    label: _$t("type"),
    align: "left",
    field: "pluginId",
  },
];

const _objectTableColumnsVisibility = ref(["name", "id", "type"]);

async function _loadDynDataSamplesTable(row) {
  try {
    const robot = new Robot(_user.token, _user.refreshToken);
    const dynDataSamplesResponse = await robot.getDynDataSamples(row.pluginId);

    _dynDataSamplesList.value = dynDataSamplesResponse.responseObject;
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
}

async function _objectTableRowSelection(ev) {
  if (!ev.added) {
    _dynDataSamplesList.value = [];
    _dynDataSelected.value = [];
    return;
  }

  const row = ev.rows[0];

  await _loadDynDataSamplesTable(row);
}

async function _objectTableRowClick(ev, row, index) {
  _objectSelected.value = [row];

  await _loadDynDataSamplesTable(row);
}

// Dynamic data table
const _dynDataTableColumnsDef = [
  {
    name: "internalName",
    align: "left",
    field: "internalName",
  },
  {
    name: "name",
    label: _$t("name"),
    align: "left",
    field: "name",
  },
  {
    name: "exampleValue",
    label: _$t("exampleValue"),
    align: "left",
    field: "exampleValue",
  },
];

const _dynDataSamplesList = ref([]);

const _dynDataTableColumnsVisibility = ref(["name", "exampleValue"]);

function _dynDataTableRowClick(ev, row, index) {
  _dynDataSelected.value = [row];
}
</script>
