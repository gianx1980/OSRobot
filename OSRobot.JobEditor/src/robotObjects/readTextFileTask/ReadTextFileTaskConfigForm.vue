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
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_propsRef.modelValue.rowFilter"
              val="ReadAllTheRows"
              :label="_$t('readAllTheRows')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.skipFirstLine"
              :label="_$t('skipFirstLine')"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadAllTheRows'"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-md">
          <div class="col">
            <q-radio
              v-model="_propsRef.modelValue.rowFilter"
              val="ReadTheLastRow"
              :label="_$t('readTheLastRow')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_propsRef.modelValue.rowFilter"
              val="ReadRowNumber"
              :label="_$t('readRowNumber')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-11">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.readRowNumber"
              :label="_$t('rowNumber')"
              lazy-rules
              dense
              :disable="_propsRef.modelValue.rowFilter !== 'ReadRowNumber'"
              :rules="[
                (val) =>
                  (_propsRef.modelValue.rowFilter === 'ReadRowNumber' &&
                    !!val) ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadRowNumber'"
              modelValueKey="readRowNumber"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_propsRef.modelValue.rowFilter"
              val="ReadInterval"
              :label="_$t('readAnInterval')"
              left-label
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
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
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
          <div class="col-11">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.readFromRow"
              :label="_$t('fromRow')"
              lazy-rules
              dense
              :rules="[
                (val) =>
                  (_propsRef.modelValue.rowFilter === 'ReadRowNumber' &&
                    !!val) ||
                  _$t('thisFieldIsMandatory'),
              ]"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadRowNumber'"
              modelValueKey="readFromRow"
            />
          </div>
        </div>
        <div
          class="row"
          v-if="_propsRef.modelValue.readInterval === 'ReadFromRowToRow'"
        >
          <div class="col-11">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.readToRow"
              :label="_$t('toRow')"
              lazy-rules
              dense
              :rules="[
                (val) =>
                  (_propsRef.modelValue.rowFilter === 'ReadRowNumber' &&
                    !!val) ||
                  _$t('thisFieldIsMandatory'),
              ]"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
              modelValueKey="readToRow"
            />
          </div>
        </div>
        <div
          class="row"
          v-if="_propsRef.modelValue.readInterval === 'ReadLastNRows'"
        >
          <div class="col-11">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.readNumberOfRows"
              :label="_$t('numberOfRows')"
              lazy-rules
              dense
              :rules="[
                (val) =>
                  (_propsRef.modelValue.rowFilter === 'ReadRowNumber' &&
                    !!val) ||
                  _$t('thisFieldIsMandatory'),
              ]"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              :disable="_propsRef.modelValue.rowFilter !== 'ReadInterval'"
              modelValueKey="readNumberOfRows"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">
          {{ _$t("columnsSplit") }}
        </div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.splitColumnsType"
              :options="_splitTypes"
              :label="_$t('splitType')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
      </q-card-section>
      <q-card-section
        v-if="_propsRef.modelValue.splitColumnsType === 'UseDelimiters'"
      >
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.delimiterTab"
              :label="_$t('tab')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.delimiterComma"
              :label="_$t('comma')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.delimiterSemicolon"
              :label="_$t('semicolon')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.delimiterSpace"
              :label="_$t('space')"
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col-4">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.delimiterOther"
              :label="_$t('other')"
              dense
            />
          </div>
          <div class="col-4">
            <q-input
              ref="inputDelimitedOtherChar"
              filled
              maxlength="1"
              :readonly="!_propsRef.modelValue.delimiterOther"
              class="q-pr-xs"
              v-model="_propsRef.modelValue.delimiterOtherChar"
              :label="_$t('otherDelimiter')"
              lazy-rules
              dense
              :rules="[
                (val) =>
                  !!val ||
                  !_propsRef.modelValue.delimiterOther ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.useDoubleQuotes"
              :label="_$t('textEnclosedInDoubleQuotes')"
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card
      class="q-mt-sm q-mb-sm"
      v-if="
        _propsRef.modelValue.splitColumnsType === 'UseDelimiters' ||
        _propsRef.modelValue.splitColumnsType === 'UseFixedWidthColumns'
      "
    >
      <q-card-section>
        <div class="text-h6">{{ _$t("columnsDefinition") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-table
              :columns="_fileColumnsDef"
              :rows="_fileColumnsList"
              :visible-columns="_fileColumnsVisibility"
              :no-data-label="_$t('thereAreNoColumnsDefined')"
              row-key="id"
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
          <div class="col" align="right">
            <q-btn
              color="primary"
              icon="upload_file"
              :label="_$t('parseDelimitedFile')"
              class="q-mt-sm q-ml-sm"
              size="md"
              @click="_showParserDialog"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
  <q-dialog v-model="_columnDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 750px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _columnDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_columnDialogFormSubmit">
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_columnDialogFormData.columnName"
                  :label="_$t('name')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-select
                  v-model="_columnDialogFormData.columnDataType"
                  :options="_dataTypes"
                  :label="_$t('dataType')"
                  dense
                  lazy-rules
                  map-options
                  emit-value
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  right-label
                  v-model="_columnDialogFormData.columnIsIdentity"
                  :label="_$t('isIdentity')"
                  dense
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_columnDialogFormData.columnExpectedCulture"
                  :label="_$t('expectedCulture')"
                  dense
                  lazy-rules
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_columnDialogFormData.columnExpectedFormat"
                  :label="_$t('expectedFormat')"
                  dense
                  lazy-rules
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  right-label
                  v-model="_columnDialogFormData.columnTreatNullStringAsNull"
                  :label="_$t('treatNullStringAsNullValue')"
                  dense
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  v-if="
                    _propsRef.modelValue.splitColumnsType === 'UseDelimiters'
                  "
                  type="number"
                  filled
                  v-model="_columnDialogFormData.columnPosition"
                  :label="_$t('columnNumber')"
                  dense
                  lazy-rules
                  :rules="[
                    (val) => {
                      return (
                        _propsRef.modelValue.splitColumnsType !==
                          'UseDelimiters' ||
                        !!val ||
                        _$t('thisFieldIsMandatory')
                      );
                    },
                  ]"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  v-if="
                    _propsRef.modelValue.splitColumnsType ===
                    'UseFixedWidthColumns'
                  "
                  type="number"
                  filled
                  v-model="_columnDialogFormData.columnStartsFromCharPos"
                  :label="_$t('startsFromCharacterN')"
                  dense
                  lazy-rules
                  :rules="[
                    (val) => {
                      return (
                        _propsRef.modelValue.splitColumnsType !==
                          'UseFixedWidthColumns' ||
                        !!val ||
                        _$t('thisFieldIsMandatory')
                      );
                    },
                  ]"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  v-if="
                    _propsRef.modelValue.splitColumnsType ===
                    'UseFixedWidthColumns'
                  "
                  type="number"
                  filled
                  v-model="_columnDialogFormData.columnEndsAtCharPos"
                  :label="_$t('endsAtCharacterN')"
                  dense
                  lazy-rules
                  :rules="[
                    (val) => {
                      return (
                        _propsRef.modelValue.splitColumnsType !==
                          'UseFixedWidthColumns' ||
                        !!val ||
                        _$t('thisFieldIsMandatory')
                      );
                    },
                  ]"
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
import ReadTextFileParserDialog from "src/robotObjects/readTextFileTask/ReadTextFileParserDialog.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);
const inputDelimitedOtherChar = ref(null);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _intervalTypes = [
  { label: _$t("readFromRowToRowPH"), value: "ReadFromRowToRow" },
  { label: _$t("readFromRowToLastRowPH"), value: "ReadFromRowToLastRow" },
  { label: _$t("readLastNRowsPH"), value: "ReadLastNRows" },
];

const _splitTypes = [
  { label: _$t("none"), value: "None" },
  { label: _$t("useDelimiters"), value: "UseDelimiters" },
  { label: _$t("useFixedWidthColumns"), value: "UseFixedWidthColumns" },
];

const _fileColumnsDef = [
  {
    name: "columnName",
    label: _$t("name"),
    align: "left",
    field: "columnName",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _fileColumnsVisibility = ref(["columnName", "actions"]);

const _fileColumnsList = ref(_propsRef.value.modelValue.columnsDefinition);

function _columnAddItemClick() {
  const list = _propsRef.value.modelValue.columnsDefinition;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _columnDialogFormData.value = {
    id: maxId,
    columnName: null,
    columnDataType: "String",
    columnIsIdentity: false,
    columnExpectedCulture: null,
    columnExpectedFormat: null,
    columnTreatNullStringAsNull: true,
    columnNumber: null,
    columnStartsFromCharacterN: null,
    columnEndsAtCharacterN: null,
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

// Handle "rowFilter" property
if (!_propsRef.value.modelValue.hasOwnProperty("rowFilter")) {
  _propsRef.value.modelValue.rowFilter = "ReadAllTheRows";
}

watch(
  () => _propsRef.value.modelValue.splitColumnsType,
  () => {
    // Reset column grid content
    _propsRef.value.modelValue.columnsDefinition.length = 0;
  }
);

watch(
  () => _propsRef.value.modelValue.readRowType,
  (newValue) => {
    switch (newValue) {
      case "ReadAllTheRows":
        _propsRef.value.modelValue.readAllTheRowsOption = true;
        _propsRef.value.modelValue.readLastRowOption = false;
        _propsRef.value.modelValue.readRowNumberOption = false;
        _propsRef.value.modelValue.readIntervalOption = false;
        break;

      case "ReadLastRow":
        _propsRef.value.modelValue.readAllTheRowsOption = false;
        _propsRef.value.modelValue.readLastRowOption = true;
        _propsRef.value.modelValue.readRowNumberOption = false;
        _propsRef.value.modelValue.readIntervalOption = false;
        break;

      case "ReadRowNumber":
        _propsRef.value.modelValue.readAllTheRowsOption = false;
        _propsRef.value.modelValue.readLastRowOption = false;
        _propsRef.value.modelValue.readRowNumberOption = true;
        _propsRef.value.modelValue.readIntervalOption = false;
        break;

      case "ReadInterval":
        _propsRef.value.modelValue.readAllTheRowsOption = false;
        _propsRef.value.modelValue.readLastRowOption = false;
        _propsRef.value.modelValue.readRowNumberOption = false;
        _propsRef.value.modelValue.readIntervalOption = true;
        break;
    }
  }
);

watch(
  () => _propsRef.value.modelValue.delimiterOther,
  () => {
    // Reset validation of input delimitedOtherChar
    _propsRef.value.modelValue.delimiterOtherChar = null;
    inputDelimitedOtherChar.value.resetValidation();
  }
);

// Dialog management
const _columnDialogVisibility = ref(false);
const _columnDialogFormData = ref({});
const _columnDialogTitle = ref(_$t("addColumn"));

const _dataTypes = [
  { label: _$t("string"), value: "String" },
  { label: _$t("integer"), value: "Integer" },
  { label: _$t("decimal"), value: "Decimal" },
  { label: _$t("datetime"), value: "Datetime" },
];

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

function _showParserDialog() {
  _$q
    .dialog({
      component: ReadTextFileParserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
      },
    })
    .onOk((ev) => {
      // Reset current column configuration e set new configuration
      _propsRef.value.modelValue.splitColumnsType = "UseDelimiters";
      _propsRef.value.modelValue.delimiterTab = false;
      _propsRef.value.modelValue.delimiterComma = false;
      _propsRef.value.modelValue.delimiterSemicolon = false;
      _propsRef.value.modelValue.delimiterSpace = false;
      _propsRef.value.modelValue.delimiterOther = false;
      _propsRef.value.modelValue.delimiterOtherChar = null;
      _propsRef.value.modelValue.columnsDefinition.length = 0;

      if (ev.data.delimiter === "\t")
        _propsRef.value.modelValue.delimiterTab = true;
      else if (ev.data.delimiter === ",")
        _propsRef.value.modelValue.delimiterComma = true;
      else if (ev.data.delimiter === ";")
        _propsRef.value.modelValue.delimiterSemicolon = true;
      else if (ev.data.delimiter === " ")
        _propsRef.value.modelValue.delimiterSpace = true;
      else {
        _propsRef.value.modelValue.delimiterOther = true;
        _propsRef.value.modelValue.delimiterOtherChar = ev.data.delimiter;
      }

      let columnCount = 0;
      ev.data.columns.forEach((column) => {
        const newColumnConfig = {
          id: columnCount,
          columnName: column.columnName,
          columnDataType: column.columnType,
          columnIsIdentity: false,
          columnExpectedCulture: null,
          columnExpectedFormat: null,
          columnTreatNullStringAsNull: true,
          columnNumber: columnCount,
          columnStartsFromCharacterN: null,
          columnEndsAtCharacterN: null,
          isNew: false,
        };
        columnCount++;

        _propsRef.value.modelValue.columnsDefinition.push(newColumnConfig);
      });
    });
}
</script>
