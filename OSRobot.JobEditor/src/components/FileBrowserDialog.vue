<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 1100px; max-width: 1100px">
      <q-card-section>
        <div class="text-h6">{{ _$t("browseFiles") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="q-pa-md q-gutter-sm" style="height: 500px; overflow: auto">
          <q-splitter v-model="_splitterModel" style="height: 450px">
            <template v-slot:before>
              <q-tree
                :nodes="_nodes"
                default-expand-all
                node-key="fullPath"
                selected-color="primary"
                :no-selection-unset="true"
                @lazy-load="_treeLazyLoad"
                v-model:selected="_selectedFolder"
              />
            </template>
            <template v-slot:after>
              <q-table
                selection="single"
                :rows="_fileList"
                :columns="_fileColumnsDef"
                v-model:selected="_fileSelected"
                dense
                row-key="name"
                @row-click="_fileTableRowClick"
              />
            </template>
          </q-splitter>
        </div>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('cancel')" @click="onDialogCancel" />
        <q-btn
          color="primary"
          :label="_$t('selectFolder')"
          @click="_onSelectFolderClick"
          :disable="_selectedFolder.length === 0"
        />
        <q-btn
          color="primary"
          :label="_$t('selectFile')"
          @click="_onSelectFileClick"
          :disable="_fileSelected.length === 0"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, watch, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import ClientUtils from "src/infrastructure/server/ClientUtils.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();
const _router = useRouter();

const _nodes = ref([]);
const _selectedFolder = ref("");

const _splitterModel = ref(25);

const _fileSelected = ref([]);

let _directorySeparatorChar = null;

const _fileColumnsDef = [
  {
    name: "name",
    field: "name",
    label: _$t("fileName"),
    align: "left",
    sortable: true,
  },
];

const _fileList = ref([]);

defineEmits([
  // REQUIRED; need to specify some events that your
  // component will emit through useDialogPluginComponent()
  ...useDialogPluginComponent.emits,
]);

const { dialogRef, onDialogHide, onDialogOK, onDialogCancel } =
  useDialogPluginComponent();

function _onSelectFolderClick() {
  if (_selectedFolder.value.length === 0) return;

  const result = _selectedFolder.value;

  const selection = {
    selectedValue: result,
  };

  onDialogOK(selection);
}

function _onSelectFileClick() {
  if (_fileSelected.value.length === 0) return;

  const result = _selectedFolder.value + _fileSelected.value[0].name;

  const selection = {
    selectedValue: result,
  };

  onDialogOK(selection);
}

function _combinePath(path1, path2) {
  if (!path1.endsWith(_directorySeparatorChar))
    return path1 + _directorySeparatorChar + path2;
  else return path1 + path2;
}

function _buildNodeFullPath(node) {
  let fullPath = node.label;

  node = node.parent;

  while (node) {
    fullPath = _combinePath(node.label, fullPath);
    node = node.parent;
  }

  return fullPath;
}

async function _treeLazyLoad({ node, key, done, fail }) {
  try {
    const clientUtils = new ClientUtils(_user.token, _user.refreshToken);
    const folderListResponse = await clientUtils.getFolderList(node.fullPath);
    const parentFullPath = _buildNodeFullPath(node);

    const newNodes = folderListResponse.responseObject.map((folderName) => {
      return {
        fullPath: _combinePath(parentFullPath, folderName),
        label: folderName,
        expandable: true,
        lazy: true,
        parent: node,
      };
    });

    done(newNodes);
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);

    fail();
  }
}

watch(_selectedFolder, async (newSelectedFolder) => {
  try {
    const clientUtils = new ClientUtils(_user.token, _user.refreshToken);
    const fileListResponse = await clientUtils.getFileList(newSelectedFolder);

    _fileList.value = fileListResponse.responseObject.map((fileName) => {
      return {
        name: fileName,
      };
    });
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});

function _fileTableRowClick(ev, row, index) {
  _fileSelected.value = [row];
}

onMounted(async () => {
  try {
    const clientUtils = new ClientUtils(_user.token, _user.refreshToken);

    _nodes.value = [];
    const driveListResponse = await clientUtils.getDriveList();
    for (const drive of driveListResponse.responseObject) {
      const driveName = drive.name.endsWith(_directorySeparatorChar)
        ? drive.name.substr(0, drive.name.length - 1)
        : drive.name;

      _nodes.value.push({
        fullPath: driveName,
        label: driveName,
        expandable: true,
        lazy: true,
        parent: null,
      });
    }

    const osInfo = await clientUtils.getOSInfo();
    _directorySeparatorChar = osInfo.responseObject.directorySeparatorChar;
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});
</script>
