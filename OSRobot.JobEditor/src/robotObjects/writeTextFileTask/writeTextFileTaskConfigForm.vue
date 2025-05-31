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
        <div class="row" v-if="_propsRef.modelValue.taskType === 'ReplaceText'">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.findText"
              :label="_$t('findText')"
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
              modelValueKey="findText"
            />
          </div>
        </div>
        <div class="row" v-if="_propsRef.modelValue.taskType === 'ReplaceText'">
          <div class="col-9">
            <q-input
              filled
              v-model="_propsRef.modelValue.replaceWithText"
              :label="_$t('replaceWithText')"
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
              modelValueKey="replaceWithText"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card
      class="q-mt-sm q-mb-sm"
      v-if="
        _propsRef.modelValue.taskType === 'InsertRow' ||
        _propsRef.modelValue.taskType === 'AppendRow'
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
        <div class="row q-mb-sm">
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
        <div class="row">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.formatAsDelimitedFile"
              :label="_$t('formatAsDelimitedFile')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm q-mb-sm">
              <q-card-section>
                <div class="text-h6">{{ _$t("delimiters") }}</div>
              </q-card-section>
              <q-card-section>
                <div class="row q-mb-sm">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.delimiterTab"
                      :label="_$t('tab')"
                      :disable="!_propsRef.modelValue.formatAsDelimitedFile"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-sm">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.delimiterSemicolon"
                      :label="_$t('semicolon')"
                      :disable="!_propsRef.modelValue.formatAsDelimitedFile"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-sm">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.delimiterComma"
                      :label="_$t('comma')"
                      :disable="!_propsRef.modelValue.formatAsDelimitedFile"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-sm">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.delimiterSpace"
                      :disable="!_propsRef.modelValue.formatAsDelimitedFile"
                      :label="_$t('space')"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-sm">
                  <div class="col">
                    <q-toggle
                      v-model="_propsRef.modelValue.delimiterOther"
                      :disable="!_propsRef.modelValue.formatAsDelimitedFile"
                      :label="_$t('other')"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row q-mb-sm">
                  <div class="col-3">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.delimiterOtherChar"
                      :disable="
                        !_propsRef.modelValue.formatAsDelimitedFile ||
                        !_propsRef.modelValue.delimiterOther
                      "
                      :label="_$t('other')"
                      lazy-rules
                      dense
                      :rules="[
                        (val) =>
                          !_propsRef.modelValue.delimiterOther ||
                          !!val ||
                          _$t('thisFieldIsMandatory'),
                      ]"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.formatAsFixedLengthColumnsFile"
              :label="_$t('formatAsFixedLengthColumnsFile')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.addHeaderIfEmpty"
              :label="_$t('addHeaderIfFileEmpty')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row" v-if="_propsRef.modelValue.taskType === 'InsertRow'">
          <div class="col-10">
            <q-input
              filled
              v-model="_propsRef.modelValue.insertAtRow"
              :label="_$t('insertAtRow')"
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
              modelValueKey="insertAtRow"
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
                  :label="_$t('headerTitle')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_columnDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  :pluginId="_propsRef.modelValue.id"
                  modelValueKey="headerTitle"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_columnDialogFormData.fieldValue"
                  :label="_$t('fieldValue')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_columnDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  :pluginId="_propsRef.modelValue.id"
                  modelValueKey="fieldValue"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_columnDialogFormData.fieldWidth"
                  :label="_$t('fieldWidth')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_columnDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  :pluginId="_propsRef.modelValue.id"
                  modelValueKey="fieldWidth"
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
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import { useAppStore } from "src/stores/appStore.js";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

// Copy path command management
const _taskTypes = [
  { label: _$t("appendRow"), value: "AppendRow" },
  { label: _$t("insertRow"), value: "InsertRow" },
  { label: _$t("replaceText"), value: "ReplaceText" },
];

const _fileColumnsDef = [
  {
    name: "headerTitle",
    label: _$t("headerTitle"),
    align: "left",
    field: "headerTitle",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _fileColumnsVisibility = ref(["headerTitle", "actions"]);

const _fileColumnsList = ref(_propsRef.value.modelValue.columnsDefinition);

function _columnAddItemClick() {
  const list = _propsRef.value.modelValue.columnsDefinition;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _columnDialogFormData.value = {
    id: maxId,
    headerTitle: null,
    fieldValue: null,
    fieldWidth: null,
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
</script>
