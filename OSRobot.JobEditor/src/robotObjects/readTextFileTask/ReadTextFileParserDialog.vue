<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card
      class="q-dialog-plugin"
      style="width: 900px; max-width: 900px; height: 670px"
    >
      <q-card-section>
        <div class="text-h6">{{ _$t("parseDelimitedFile") }}</div>
      </q-card-section>
      <q-card-section>
        <q-stepper
          v-model="_wizardCurrentStepIndex"
          ref="_stepper"
          color="primary"
          animated
        >
          <q-step
            :name="0"
            :title="_$t('uploadAFile')"
            icon="upload_file"
            :error="_stepSelectFileError"
            :done="_wizardCurrentStepIndex > 0"
          >
            <div
              class="q-pa-md q-gutter-sm col-12"
              style="height: 350px; overflow: auto"
            >
              <div class="row">
                <div class="col-12">
                  {{ _$t("selectAFileToParseToAutodetectConfig") }}
                  <q-file
                    ref="_fileToUpload"
                    :model-value="_selectedFile"
                    @update:model-value="_updateFile"
                    :label="_$t('clickToSelectAFile')"
                    outlined
                    style="max-width: 780px"
                    :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    lazy-rules
                  ></q-file>
                </div>
                <div class="col-12">
                  <q-checkbox
                    v-model="_firstRowHasColumnNames"
                    :label="_$t('firstRowHasColumnNames')"
                  >
                  </q-checkbox>
                </div>
              </div>
            </div>
          </q-step>

          <q-step
            :name="1"
            title="Preview"
            icon="preview"
            :done="_wizardCurrentStepIndex > 1"
          >
            <div
              class="q-pa-md q-gutter-sm"
              style="height: 350px; overflow: auto; text-align: center"
            >
              <div v-if="_parseStatus === 'inProgress'">
                <div>{{ _parseRowsRead }} {{ _$t("rowsRead") }}</div>
                <div>
                  <q-circular-progress
                    indeterminate
                    size="90px"
                    :thickness="0.2"
                    color="white"
                    track-color="primary"
                    class="q-ma-md"
                  />
                </div>
                <div>
                  <q-btn
                    @click="_stopParsingCSV"
                    color="negative"
                    :label="_$t('stopParsing')"
                  />
                </div>
              </div>
              <div v-if="_parseStatus === 'completed'">
                <div class="row">
                  <div class="col-12" align="left">
                    <b>{{ _$t("numberOfColumns") }}:</b>
                    {{ _numColumnsDetected }}
                  </div>
                  <div class="col-12" align="left">
                    <b>{{ _$t("delimiter") }}:</b>
                    {{ _getDelimiterDescription(_delimiterDetected) }}
                  </div>
                </div>
                <div class="row">
                  <div class="col-12">
                    <q-table
                      title="Detected columns"
                      dense
                      flat
                      bordered
                      height="200px"
                      :rows="_tableRows"
                      :columns="_tableColumns"
                      row-key="columnName"
                    />
                  </div>
                </div>
              </div>
            </div>
          </q-step>

          <template v-slot:navigation>
            <div v-if="_showStepperNavigationButtons" class="q-pb-sm">
              <q-stepper-navigation align="center">
                <q-btn
                  @click="_stepperNext"
                  color="primary"
                  :label="
                    _wizardCurrentStepIndex === 1
                      ? _$t('useDetectedConfiguration')
                      : _$t('continue')
                  "
                />
                <q-btn
                  v-if="_wizardCurrentStepIndex > 0"
                  flat
                  color="primary"
                  @click="_stepperPrevious"
                  :label="_$t('back')"
                  class="q-ml-sm"
                />
              </q-stepper-navigation>
            </div>
          </template>
        </q-stepper>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn @click="_cancelClick" color="primary" :label="_$t('cancel')" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, onMounted, computed } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import Papa from "papaparse";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();
const _router = useRouter();

const _fileToUpload = ref(null);
const _firstRowHasColumnNames = ref(false);

const _stepper = ref(null);
const _stepSelectFileIndex = 0;
const _stepSelectFileError = ref(null);
const _stepPreview = 1;

const _wizardCurrentStepIndex = ref(0);
const _parseRowsRead = ref(0);
const _parseStatus = ref("inProgress");
let _parseResult = null;

let _CSVParser = null;

const _selectedFile = ref(null);
function _updateFile(newFile) {
  _selectedFile.value = newFile;
}

const _tableColumns = [
  {
    name: "columnName",
    label: "Column name",
    align: "left",
    field: (row) => row.columnName,
  },
  {
    name: "columnType",
    label: "Column type",
    align: "left",
    field: (row) => row.columnType,
  },
];

const _numColumnsDetected = ref(0);
const _delimiterDetected = ref("");
const _tableRows = ref([]);

defineEmits([
  // REQUIRED; need to specify some events that your
  // component will emit through useDialogPluginComponent()
  ...useDialogPluginComponent.emits,
]);

const { dialogRef, onDialogHide, onDialogOK, onDialogCancel } =
  useDialogPluginComponent();

const _showStepperNavigationButtons = computed(() => {
  return (
    _wizardCurrentStepIndex.value === _stepSelectFileIndex ||
    (_wizardCurrentStepIndex.value === _stepPreview &&
      _parseStatus.value !== "inProgress")
  );
});

function _getDelimiterDescription(delimiter) {
  if (delimiter === "\t") return _$t("tab");
  else if (delimiter === ",") return _$t("comma");
  else if (delimiter === ";") return _$t("semiColon");
  else if (delimiter === " ") return _$t("space");
  else return delimiter;
}

function _createDetectedConfigObject() {
  return {
    delimiter: "",
    columns: [],
  };
}

function _createColumnInfoObject(columnName, columnType) {
  return { columnName: columnName, columnType: columnType };
}

function _parseCSV() {
  // TODO: type detectioon to be improved!
  return new Promise((resolve, reject) => {
    const detectedConfig = _createDetectedConfigObject();

    Papa.parse(_selectedFile.value, {
      skipEmptyLines: true,
      dynamicTyping: true,
      step: function (results, parser) {
        _CSVParser = parser;

        if (_parseRowsRead.value === 0) {
          if (_firstRowHasColumnNames.value) {
            // Detect column names from first row
            for (let columnName of results.data) {
              detectedConfig.columns.push(
                _createColumnInfoObject(columnName, "String")
              );
            }
          } else {
            // If no header exists, set a default column name<
            for (let i = 0; i < results.data.length; i++) {
              detectedConfig.columns.push(
                _createColumnInfoObject("Column_" + i.toString(), "String")
              );
            }
          }

          detectedConfig.delimiter = results.meta.delimiter;
          parser.abort();
        }
      },

      error: function (error, file) {
        reject({ status: "error", data: null });

        _parseStatus.value = "error";
        _parseRowsRead.value = 0;
      },

      complete: function (results, file) {
        resolve({ status: "ok", data: detectedConfig });

        _parseStatus.value = "completed";
        _parseRowsRead.value = 0;
      },
    });
  });
}

function _stopParsingCSV() {
  if (_CSVParser && _parseStatus.value === "inProgress") _CSVParser.abort();
}

async function _manageStepSelectFile() {
  if (!_fileToUpload.value.validate()) {
    _stepSelectFileError.value = true;
  } else {
    _stepSelectFileError.value = false;
    _parseStatus.value = "inProgress";
    _parseRowsRead.value = 0;

    _parseResult = await _parseCSV();

    _numColumnsDetected.value = _parseResult.data.columns.length;
    _delimiterDetected.value = _parseResult.data.delimiter;

    //_detectedConfig.value = config.data;

    if (_parseResult.status === "ok") {
      _tableRows.value = _parseResult.data.columns;
      _parseStatus.value = "completed";
    } else {
      _parseStatus.value = "error";
    }

    _stepper.value.next();
  }
}

function _manageStepPreview() {
  _$q
    .dialog({
      title: _$t("osRobot"),
      message: _$t("theCurrentConfigurationWillBeLostContinue"),
      cancel: true,
      persistent: true,
    })
    .onOk(() => {
      onDialogOK(_parseResult);
    });
}

function _stepperNext() {
  switch (_wizardCurrentStepIndex.value) {
    case _stepSelectFileIndex:
      _manageStepSelectFile();
      break;

    case _stepPreview:
      _manageStepPreview();
      break;
  }
}

function _stepperPrevious() {
  _stepper.value.previous();
}

function _cancelClick() {
  _stopParsingCSV();
  onDialogCancel();
}

onMounted(async () => {
  try {
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});
</script>
