<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("connection") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.protocol"
              :options="_protocols"
              :label="_$t('protocol')"
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
            <q-input
              filled
              v-model="_propsRef.modelValue.host"
              :label="_$t('host')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              type="number"
              min="1"
              max="9999"
              v-model="_propsRef.modelValue.port"
              :label="_$t('port')"
              lazy-rules
              dense
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 9999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '65535']),
              ]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.username"
              :label="_$t('username')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.password"
              :label="_$t('password')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
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
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _copyPathDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_copyPathDialogFormSubmit">
            <div class="row">
              <div class="col">
                <q-select
                  v-model="_copyPathDialogFormData.direction"
                  :options="_directions"
                  :label="_$t('direction')"
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
                  v-model="_copyPathDialogFormData.localPath"
                  :label="_$t('localPath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnFileBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  modelValueKey="localPath"
                />
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="localPath"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_copyPathDialogFormData.remotePath"
                  :label="_$t('remotePath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_copyPathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="remotePath"
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
                  v-model="_copyPathDialogFormData.recursivelyCopyDirectories"
                  :label="_$t('recursivelyCopyDirectories')"
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
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _deletePathDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_deletePathDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_deletePathDialogFormData.remotePath"
                  :label="_$t('remotePath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_deletePathDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="remotePath"
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

const _protocols = [
  { label: _$t("FTP"), value: "FTP" },
  { label: _$t("SFTP"), value: "SFTP" },
];

const _commands = [
  { label: _$t("copy"), value: "Copy" },
  { label: _$t("delete"), value: "Delete" },
];

// Copy path command management
const _directions = [
  { label: _$t("localToRemote"), value: "LocalToRemote" },
  { label: _$t("remoteToLocal"), value: "RemoteToLocal" },
];

const _copyPathColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "localPath",
    label: _$t("localPath"),
    align: "left",
    field: "localPath",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _copyPathColumnVisibility = ref(["localPath", "actions"]);

const _copyPathList = ref(_propsRef.value.modelValue.copyItems);

function _copyPathAddItemClick() {
  const list = _propsRef.value.modelValue.copyItems;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _copyPathDialogFormData.value = {
    id: maxId,
    localToRemote: true,
    localPath: "",
    remotePath: "",
    overwriteFileIfExists: false,
    recursivelyCopyDirectories: false,
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

// Handle "direction" property
if (!_propsRef.value.modelValue.hasOwnProperty("direction")) {
  _propsRef.value.modelValue.direction = "LocalToRemote";
}

watch(
  () => _propsRef.value.modelValue.direction,
  (newValue) => {
    switch (newValue) {
      case "LocalToRemote":
        _propsRef.value.modelValue.localToRemote = true;
        break;

      case "RemoteToLocal":
        _propsRef.value.modelValue.localToRemote = false;
        break;
    }
  }
);

// Dialog management
const _copyPathDialogVisibility = ref(false);
const _copyPathDialogFormData = ref({});
const _copyPathDialogTitle = ref(_$t("addColumn"));

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

// Delete command management
const _deletePathColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "remotePath",
    label: _$t("remotePath"),
    align: "left",
    field: "remotePath",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _deletePathColumnVisibility = ref(["remotePath", "actions"]);

const _deletePathList = ref(_propsRef.value.modelValue.deleteItems);

function _deletePathAddItemClick() {
  const list = _propsRef.value.modelValue.deleteItems;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _deletePathDialogFormData.value = {
    id: maxId,
    localToRemote: true,
    remotePath: "",
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

// Dialog management
const _deletePathDialogVisibility = ref(false);
const _deletePathDialogFormData = ref({});
const _deletePathDialogTitle = ref(_$t("addColumn"));

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
