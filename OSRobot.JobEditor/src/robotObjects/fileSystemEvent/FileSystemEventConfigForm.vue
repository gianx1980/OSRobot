<template>
  <div class="q-pa-md">
    <q-form class="q-gutter-md" dense>
      <PluginGeneralConfigForm
        v-model="_formData.modelValue"
        @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
      />
      <q-card class="q-mt-sm">
        <q-card-section>
          <div class="text-h6">{{ _$t("folderCheck") }}</div>
        </q-card-section>
        <q-card-section>
          <div class="row">
            <div class="col">
              <q-table
                :columns="_pathColumnsDef"
                :rows="_pathCheckList"
                :visible-columns="_pathColumnsVisibility"
                :no-data-label="_$t('thereAreNoFoldersToCheck')"
                row-key="name"
                dense
              >
                <template v-slot:body-cell-actions="props">
                  <q-btn square size="sm" icon="mode_edit" color="primary">
                    <q-menu>
                      <q-list style="min-width: 100px">
                        <q-item clickable v-close-popup>
                          <q-item-section
                            @click="_folderEditItemClick(props.row)"
                            >{{ _$t("edit") }}</q-item-section
                          >
                        </q-item>
                        <q-item clickable v-close-popup>
                          <q-item-section
                            @click="_folderDeleteItemClick(props.row)"
                            >{{ _$t("delete") }}</q-item-section
                          >
                        </q-item>
                      </q-list>
                    </q-menu>
                  </q-btn>
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
                @click="_folderAddItemClick"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>
    </q-form>
  </div>
  <q-dialog v-model="_folderDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _folderDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_folderDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_folderDialogFormData.path"
                  :label="_$t('path')"
                  lazy-rules
                  dense
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                  maxlength="5000"
                />
              </div>
              <div class="col-1">
                <BtnFolderBrowser
                  class="q-ml-sm"
                  v-model="_folderDialogFormData"
                  modelValueKey="path"
                />
              </div>
            </div>

            <div class="row">
              <div class="col">
                <q-select
                  v-model="_folderDialogFormData.monitorAction"
                  :options="_actions"
                  :label="_$t('selectAnAction')"
                  dense
                  emit-value
                  map-options
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>

            <div class="row">
              <div class="col">
                <q-toggle
                  v-model="_folderDialogFormData.monitorSubfolders"
                  :label="_$t('monitorSubFolders')"
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
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import BtnFolderBrowser from "src/components/BtnFolderBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();
const _formData = ref(_props);

const _pathColumnsDef = [
  {
    name: "path",
    label: _$t("path"),
    align: "left",
    field: "path",
    sortable: true,
  },
  {
    name: "monitorAction",
    label: _$t("action"),
    align: "left",
    field: "monitorAction",
    format: (val, row) => _actions.filter((v) => v.value === val)[0].label,
    sortable: true,
  },
  {
    name: "monitorSubfolders",
    label: _$t("monitorSubFolders"),
    align: "left",
    field: "monitorSubfolders",
    format: (val, row) => (val ? _$t("yes") : _$t("no")),
    sortable: true,
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
    sortable: false,
  },
];

const _pathColumnsVisibility = ref([
  "path",
  "monitorAction",
  "monitorSubfolders",
  "actions",
]);

const _pathCheckList = ref(_formData.value.modelValue.foldersToMonitor);

function _folderAddItemClick() {
  const list = _formData.value.modelValue.foldersToMonitor;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _folderDialogFormData.value = {
    id: maxId,
    path: "",
    monitorAction: "",
    monitorSubfolders: false,
    isNew: true,
  };

  _folderDialogTitle.value = _$t("addFolderCheck");
  _folderDialogVisibility.value = true;
}

function _folderEditItemClick(row) {
  _folderDialogFormData.value = Object.assign({}, row);
  _folderDialogTitle.value = _$t("editFolderCheck");
  _folderDialogVisibility.value = true;
}

function _folderDeleteItemClick(row) {
  const index = _formData.value.modelValue.foldersToMonitor.findIndex(
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
        _formData.value.modelValue.foldersToMonitor.splice(index, 1);
      }
    });
}

watch(
  () => _formData.value.modelValue.name,
  (newName, oldName) => {
    const eventArgs = {
      nodeId: _formData.value.modelValue.id,
      newName: newName,
      oldName: oldName,
    };
    _emit("nodeNeedsUpdate", eventArgs);
  }
);

//onMounted(async () => {});

// Dialog management
const _folderDialogVisibility = ref(false);
const _folderDialogFormData = ref({});
const _folderDialogTitle = ref(_$t("addFolderCheck"));
const _actions = [
  { label: _$t("newFiles"), value: "NewFiles" },
  { label: _$t("modifiedFiles"), value: "ModifiedFiles" },
  { label: _$t("deletedFiles"), value: "DeletedFiles" },
];

function _folderDialogFormSubmit() {
  if (_folderDialogFormData.value.isNew) {
    _folderDialogFormData.value.isNew = false;
    _formData.value.modelValue.foldersToMonitor.push(
      _folderDialogFormData.value
    );
  } else {
    const index = _formData.value.modelValue.foldersToMonitor.findIndex(
      (i) => i.id === _folderDialogFormData.value.id
    );

    _formData.value.modelValue.foldersToMonitor[index] =
      _folderDialogFormData.value;
  }

  _folderDialogVisibility.value = false;
}
</script>
