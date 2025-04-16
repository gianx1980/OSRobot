<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("action") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.taskType"
              :options="_taskTypes"
              :label="_$t('type')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.filePath"
              :label="_$t('filePath')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-3">
            <BtnFileBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              modelValueKey="filePath"
            />
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="filePath"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.sheetName"
              :label="_$t('sheetName')"
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
              modelValueKey="sheetName"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm" v-if="_appendInsertFieldsetVisibile">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("insertAppendConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_sheetColumnsDef"
                      :rows="_sheetColumnsList"
                      :visible-columns="_sheetColumnsVisibility"
                      :no-data-label="_$t('thereAreNoColumnsDefined')"
                      row-key="cellValue"
                      dense
                    >
                      <template v-slot:body-cell-actions="props">
                        <q-td :props="props">
                          <q-btn
                            square
                            class="q-ml-xs"
                            size="xs"
                            icon="mode_edit"
                            color="primary"
                          >
                            <q-menu>
                              <q-list style="min-width: 100px">
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_columnEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_columnDeleteItemClick(props.row)"
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_columnItemMoveUpClick(props.row)"
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_columnItemMoveDownClick(props.row)"
                                    >{{ _$t("moveDown") }}</q-item-section
                                  >
                                </q-item>
                              </q-list>
                            </q-menu>
                          </q-btn>
                        </q-td>
                      </template>
                    </q-table>
                  </div>
                </div>
                <div class="row">
                  <div class="col">
                    <q-btn
                      color="primary"
                      icon="add"
                      :label="_$t('add')"
                      class="q-mt-sm"
                      size="md"
                      @click="_columnAddItemClick"
                    />
                  </div>
                </div>
                <div class="row q-mt-md">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.addHeaderIfEmpty"
                      :label="_$t('addHeaderIfFileEmpty')"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div
                  class="row q-mt-md"
                  v-if="_propsRef.modelValue.taskType == 'InsertRow'"
                >
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.insertAtRow"
                      :label="_$t('insertAtRow')"
                      lazy-rules
                      dense
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="insertAtRow"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card class="q-mt-sm" v-if="_readFieldsetVisible">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("readConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row q-mb-xs">
                  <div class="col">
                    <q-radio
                      v-model="_propsRef.modelValue.readRowType"
                      val="ReadLastRow"
                      :label="_$t('readTheLastRow')"
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-xs">
                  <div class="col">
                    <q-radio
                      v-model="_propsRef.modelValue.readRowType"
                      val="ReadRowNumber"
                      :label="_$t('readRowNumber')"
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mt-xs">
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.readRowNumber"
                      :label="_$t('rowNumber')"
                      lazy-rules
                      dense
                      :disable="
                        _propsRef.modelValue.readRowType !== 'ReadRowNumber'
                      "
                      :rules="[
                        (val) =>
                          (_propsRef.modelValue.readRowType ===
                            'ReadRowNumber' &&
                            !!val) ||
                          _$t('thisFieldIsMandatory'),
                      ]"
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="readRowNumber"
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col">
                    <q-radio
                      v-model="_propsRef.modelValue.readRowType"
                      val="ReadAnInterval"
                      :label="_$t('readAnInterval')"
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-xs">
                  <div class="col">
                    <q-select
                      v-model="_propsRef.modelValue.readInterval"
                      :options="_intervalTypes"
                      :label="_$t('interval')"
                      dense
                      :disable="
                        _propsRef.modelValue.readRowType !== 'ReadAnInterval'
                      "
                      lazy-rules
                      map-options
                      emit-value
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                </div>
                <div
                  class="row"
                  v-if="
                    _propsRef.modelValue.readInterval === 'ReadFromRowToRow' ||
                    _propsRef.modelValue.readInterval === 'ReadFromRowToLastRow'
                  "
                >
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.readFromRow"
                      :label="_$t('fromRow')"
                      lazy-rules
                      dense
                      :rules="[
                        (val) =>
                          (_propsRef.modelValue.readInterval ===
                            'ReadFromRowToLastRow' &&
                            !!val) ||
                          _$t('thisFieldIsMandatory'),
                      ]"
                      :disable="
                        _propsRef.modelValue.readRowType !== 'ReadAnInterval'
                      "
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="readFromRow"
                    />
                  </div>
                </div>
                <div
                  class="row"
                  v-if="
                    _propsRef.modelValue.readInterval === 'ReadFromRowToRow'
                  "
                >
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.readToRow"
                      :label="_$t('toRow')"
                      lazy-rules
                      dense
                      :rules="[
                        (val) =>
                          (_propsRef.modelValue.readRowType ===
                            'ReadRowNumber' &&
                            !!val) ||
                          _$t('thisFieldIsMandatory'),
                      ]"
                      :disable="
                        _propsRef.modelValue.readRowType !== 'ReadAnInterval'
                      "
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="readToRow"
                    />
                  </div>
                </div>
                <div
                  class="row"
                  v-if="_propsRef.modelValue.readInterval === 'ReadLastNRows'"
                >
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.readNumberOfRows"
                      :label="_$t('numberOfRows')"
                      lazy-rules
                      dense
                      :rules="[
                        (val) =>
                          (_propsRef.modelValue.readRowType ===
                            'ReadRowNumber' &&
                            !!val) ||
                          _$t('thisFieldIsMandatory'),
                      ]"
                      :disable="
                        _propsRef.modelValue.readRowType !== 'ReadAnInterval'
                      "
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="readNumberOfRows"
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col-10">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.numColumnsToRead"
                      :label="_$t('columnsToReadEmptyToReadAll')"
                      lazy-rules
                      dense
                    />
                  </div>
                  <div class="col-2">
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="numColumnsToRead"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
  <q-dialog v-model="_columnDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _columnDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_columnDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_columnDialogFormData.headerTitle"
                  :label="_$t('header')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_columnDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="headerTitle"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_columnDialogFormData.cellValue"
                  :label="_$t('value')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_columnDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="cellValue"
                />
              </div>
            </div>
            <div class="column items-end">
              <div class="col">
                <q-btn
                  flat
                  :label="_$t('cancel')"
                  color="primary"
                  v-close-popup
                />
                <q-btn flat type="submit" :label="_$t('ok')" color="primary" />
              </div>
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </div>
  </q-dialog>
</template>
<script setup>
import { ref, watch, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _taskTypes = [
  { label: _$t("appendRow"), value: "AppendRow" },
  { label: _$t("insertRow"), value: "InsertRow" },
  { label: _$t("readRow"), value: "ReadRow" },
];

const _intervalTypes = [
  { label: _$t("readFromRowToRowPH"), value: "ReadFromRowToRow" },
  { label: _$t("readFromRowToLastRowPH"), value: "ReadFromRowToLastRow" },
  { label: _$t("readLastNRowsPH"), value: "ReadLastNRows" },
];

const _appendInsertFieldsetVisibile = computed(() => {
  return (
    _propsRef.value.modelValue.taskType === "AppendRow" ||
    _propsRef.value.modelValue.taskType === "InsertRow"
  );
});

const _readFieldsetVisible = computed(() => {
  return _propsRef.value.modelValue.taskType === "ReadRow";
});

const _sheetColumnsDef = [
  {
    name: "headerTitle",
    label: _$t("title"),
    align: "left",
    field: "headerTitle",
  },
  {
    name: "cellValue",
    label: _$t("value"),
    align: "left",
    field: "cellValue",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _sheetColumnsVisibility = ref(["headerTitle", "cellValue", "actions"]);

const _sheetColumnsList = ref(_propsRef.value.modelValue.columnsDefinition);

function _columnAddItemClick() {
  const list = _propsRef.value.modelValue.columnsDefinition;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _columnDialogFormData.value = {
    id: maxId,
    headerTitle: "",
    cellValue: "",
    isNew: true,
  };

  _columnDialogTitle.value = _$t("addColumn");
  _columnDialogVisibility.value = true;
}

function _columnEditItemClick(row) {
  _columnDialogFormData.value = Object.assign({}, row);
  _columnDialogTitle.value = _$t("editColumn");
  _columnDialogVisibility.value = true;
}

function _columnDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.columnsDefinition.findIndex(
    (i) => i.id === row.id
  );

  _$q
    .dialog({
      title: _$t("osRobot"),
      message: _$t("doYouWantToDeleteItem"),
      cancel: true,
      persistent: true,
    })
    .onOk(() => {
      if (index >= 0) {
        _propsRef.value.modelValue.columnsDefinition.splice(index, 1);
      }
    });
}

function _columnItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.columnsDefinition;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _columnItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.columnsDefinition;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Handle "readRowType" property
if (!_propsRef.value.modelValue.hasOwnProperty("readRowType")) {
  _propsRef.value.modelValue.readRowType = "ReadLastRow";
}

watch(
  () => _propsRef.value.modelValue.readRowType,
  (newValue) => {
    switch (newValue) {
      case "ReadLastRow":
        _propsRef.value.modelValue.readLastRowOption = true;
        _propsRef.value.modelValue.readRowNumberOption = false;
        _propsRef.value.modelValue.readIntervalOption = false;
        break;

      case "ReadRowNumber":
        _propsRef.value.modelValue.readLastRowOption = false;
        _propsRef.value.modelValue.readRowNumberOption = true;
        _propsRef.value.modelValue.readIntervalOption = false;
        break;

      case "ReadAnInterval":
        _propsRef.value.modelValue.readLastRowOption = false;
        _propsRef.value.modelValue.readRowNumberOption = false;
        _propsRef.value.modelValue.readIntervalOption = true;
        break;
    }
  }
);

// Dialog management
const _columnDialogVisibility = ref(false);
const _columnDialogFormData = ref({});
const _columnDialogTitle = ref(_$t("addColumn"));

function _columnDialogFormSubmit() {
  if (_columnDialogFormData.value.isNew) {
    _columnDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.columnsDefinition.push(
      _columnDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue.columnsDefinition.findIndex(
      (i) => i.id === _columnDialogFormData.value.id
    );

    _propsRef.value.modelValue.columnsDefinition[index] =
      _columnDialogFormData.value;
  }

  _columnDialogVisibility.value = false;
}
</script>
