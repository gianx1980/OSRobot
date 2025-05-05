<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("command") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.command"
              :options="_commands"
              :label="_$t('command')"
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
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'Copy'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("copyConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_copyPathColumnsDef"
                      :rows="_copyPathList"
                      :visible-columns="_copyPathColumnVisibility"
                      :no-data-label="_$t('thereAreNoPathsToShow')"
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
                                    @click="_copyPathEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_copyPathDeleteItemClick(props.row)"
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_copyPathItemMoveUpClick(props.row)"
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _copyPathItemMoveDownClick(props.row)
                                    "
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
                      @click="_copyPathAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'Delete'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("deleteConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_deletePathColumnsDef"
                      :rows="_deletePathList"
                      :visible-columns="_deletePathColumnVisibility"
                      :no-data-label="_$t('thereAreNoPathsToShow')"
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
                                    @click="_deletePathEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _deletePathDeleteItemClick(props.row)
                                    "
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _deletePathItemMoveUpClick(props.row)
                                    "
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _deletePathItemMoveDownClick(props.row)
                                    "
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
                      @click="_deletePathAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'CreateFolder'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("createFolderConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col-9">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.createFolderPath"
                      :label="_$t('path')"
                      dense
                      lazy-rules
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                  <div class="col-3">
                    <BtnFolderBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      modelValueKey="createFolderPath"
                    />
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="createFolderPath"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'CheckExistence'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("checkExistenceConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col-9">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.checkExistenceFilePath"
                      :label="_$t('pathToCheck')"
                      dense
                      lazy-rules
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                  <div class="col-3">
                    <BtnFileBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      modelValueKey="checkExistenceFilePath"
                    />
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="checkExistenceFilePath"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'List'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("listConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col-9">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.listFolderPath"
                      :label="_$t('path')"
                      dense
                      lazy-rules
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                  <div class="col-3">
                    <BtnFolderBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      modelValueKey="listFolderPath"
                    />
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="listFolderPath"
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col-12">
                    <q-toggle
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue.listFiles"
                      :label="_$t('listFiles')"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col-12">
                    <q-toggle
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue.listFolders"
                      :label="_$t('listFolders')"
                      left-label
                      dense
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col-12">
                    <q-toggle
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue.listSubfoldersContent"
                      :label="_$t('listSubfoldersContent')"
                      left-label
                      dense
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
            <q-card
              class="q-mt-sm"
              v-if="_propsRef.modelValue.command === 'Rename'"
            >
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("renameConfiguration") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col-9">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.renameFromPath"
                      :label="_$t('renameFrom')"
                      dense
                      lazy-rules
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                  <div class="col-3">
                    <BtnFolderBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      modelValueKey="renameFromPath"
                    />
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="renameFromPath"
                    />
                  </div>
                </div>
                <div class="row">
                  <div class="col-9">
                    <q-input
                      filled
                      v-model="_propsRef.modelValue.renameToPath"
                      :label="_$t('renameTo')"
                      dense
                      lazy-rules
                      :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                    />
                  </div>
                  <div class="col-3">
                    <BtnFolderBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      modelValueKey="renameToPath"
                    />
                    <BtnDynamicDataBrowser
                      class="q-ml-sm"
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="renameToPath"
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
  <q-dialog v-model="_copyPathDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _copyPathDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_copyPathDialogFormSubmit">
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.sourcePath"
                  :label="_$t('sourcePath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnFileBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  modelValueKey="sourcePath"
                />
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="sourcePath"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.destinationPath"
                  :label="_$t('destinationPath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnFileBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  modelValueKey="destinationPath"
                />
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="destinationPath"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-12">
                <b>{{ _$t("considerFilesOlderThan") }}</b>
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.filesOlderThanDays"
                  :label="_$t('days')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanDays"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.filesOlderThanHours"
                  :label="_$t('hours')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanHours"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.filesOlderThanMinutes"
                  :label="_$t('minutes')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanMinutes"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  v-model="_copyPathDialogFormData.overwriteFileIfExists"
                  :label="_$t('overwriteFileIfExists')"
                  left-label
                  dense
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  v-model="_copyPathDialogFormData.recursivelyCopy"
                  :label="_$t('recursivelyCopy')"
                  left-label
                  dense
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
  <q-dialog v-model="_deletePathDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _deletePathDialogTitle }}</div>
        </q-card-section>
        <q-card-section>
          <q-form class="q-gutter-md" @submit="_deletePathDialogFormSubmit">
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_deletePathDialogFormData.deletePath"
                  :label="_$t('path')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnFileBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  modelValueKey="path"
                />
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="path"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-12">
                <b>{{ _$t("considerFilesOlderThan") }}</b>
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_deletePathDialogFormData.filesOlderThanDays"
                  :label="_$t('days')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanDays"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_deletePathDialogFormData.filesOlderThanHours"
                  :label="_$t('hours')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanHours"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_deletePathDialogFormData.filesOlderThanMinutes"
                  :label="_$t('minutes')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="filesOlderThanMinutes"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  v-model="_deletePathDialogFormData.recursivelyDelete"
                  :label="_$t('recursivelyDelete')"
                  left-label
                  dense
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
import BtnFolderBrowser from "src/components/BtnFolderBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _commands = [
  { label: _$t("copy"), value: "Copy" },
  { label: _$t("checkExistence"), value: "CheckExistence" },
  { label: _$t("createFolder"), value: "CreateFolder" },
  { label: _$t("delete"), value: "Delete" },
  { label: _$t("list"), value: "List" },
  { label: _$t("rename"), value: "Rename" },
];

// Copy path table management
const _copyPathColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "sourcePath",
    label: _$t("path"),
    align: "left",
    field: "sourcePath",
  },
  {
    name: "filesOlderThanDays",
    field: "filesOlderThanDays",
  },
  {
    name: "filesOlderThanHours",
    field: "filesOlderThanHours",
  },
  {
    name: "filesOlderThanMinutes",
    field: "filesOlderThanMinutes",
  },
  {
    name: "overwriteFileIfExists",
    field: "overwriteFileIfExists",
  },
  {
    name: "recursivelyCopy",
    field: "recursivelyCopy",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _copyPathColumnVisibility = ref(["sourcePath", "actions"]);

const _copyPathList = ref(_propsRef.value.modelValue.copyItems);

function _copyPathAddItemClick() {
  const list = _propsRef.value.modelValue.copyItems;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _copyPathDialogFormData.value = {
    id: maxId,
    sourcePath: "",
    destinationPath: "",
    filesOlderThanDays: "",
    filesOlderThanHours: "",
    filesOlderThanMinutes: "",
    overwriteFileIfExists: false,
    recursivelyCopy: false,
    isNew: true,
  };

  _copyPathDialogTitle.value = _$t("addPath");
  _copyPathDialogVisibility.value = true;
}

function _copyPathEditItemClick(row) {
  _copyPathDialogFormData.value = Object.assign({}, row);
  _copyPathDialogTitle.value = _$t("editPath");
  _copyPathDialogVisibility.value = true;
}

function _copyPathDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.copyItems.findIndex(
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
        _propsRef.value.modelValue.copyItems.splice(index, 1);
      }
    });
}

function _copyPathItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.copyItems;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _copyPathItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.copyItems;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Copy path dialog management
const _copyPathDialogVisibility = ref(false);
const _copyPathDialogFormData = ref({});
const _copyPathDialogTitle = ref(_$t("addPath"));

function _copyPathDialogFormSubmit() {
  if (_copyPathDialogFormData.value.isNew) {
    _copyPathDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.copyItems.push(_copyPathDialogFormData.value);
  } else {
    const index = _propsRef.value.modelValue.copyItems.findIndex(
      (i) => i.id === _copyPathDialogFormData.value.id
    );

    _propsRef.value.modelValue.copyItems[index] = _copyPathDialogFormData.value;
  }

  _copyPathDialogVisibility.value = false;
}

// Delete path table management
const _deletePathColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "deletePath",
    label: _$t("path"),
    align: "left",
    field: "deletePath",
  },
  {
    name: "filesOlderThanDays",
    field: "filesOlderThanDays",
  },
  {
    name: "filesOlderThanHours",
    field: "filesOlderThanHours",
  },
  {
    name: "filesOlderThanMinutes",
    field: "filesOlderThanMinutes",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _deletePathColumnVisibility = ref(["deletePath", "actions"]);

const _deletePathList = ref(_propsRef.value.modelValue.deleteItems);

function _deletePathAddItemClick() {
  const list = _propsRef.value.modelValue.deleteItems;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _deletePathDialogFormData.value = {
    id: maxId,
    deletePath: "",
    filesOlderThanDays: "",
    filesOlderThanHours: "",
    filesOlderThanMinutes: "",
    recursivelyDelete: false,
    isNew: true,
  };

  _deletePathDialogTitle.value = _$t("addPath");
  _deletePathDialogVisibility.value = true;
}

function _deletePathEditItemClick(row) {
  _deletePathDialogFormData.value = Object.assign({}, row);
  _deletePathDialogTitle.value = _$t("editPath");
  _deletePathDialogVisibility.value = true;
}

function _deletePathDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.deleteItems.findIndex(
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
        _propsRef.value.modelValue.deleteItems.splice(index, 1);
      }
    });
}

function _deletePathItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.deleteItems;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _deletePathItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.deleteItems;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Delete path dialog management
const _deletePathDialogVisibility = ref(false);
const _deletePathDialogFormData = ref({});
const _deletePathDialogTitle = ref(_$t("addPath"));

function _deletePathDialogFormSubmit() {
  if (_deletePathDialogFormData.value.isNew) {
    _deletePathDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.deleteItems.push(
      _deletePathDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue.deleteItems.findIndex(
      (i) => i.id === _deletePathDialogFormData.value.id
    );

    _propsRef.value.modelValue.deleteItems[index] =
      _deletePathDialogFormData.value;
  }

  _deletePathDialogVisibility.value = false;
}
</script>
