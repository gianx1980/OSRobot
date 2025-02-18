<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card
      class="q-dialog-plugin"
      style="width: 700px; max-width: 80vw; height: 500px"
    >
      <q-card-section>
        <div class="text-h6">{{ _$t("browseFolder") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="q-pa-md q-gutter-sm" style="height: 350px; overflow: auto">
          <q-tree
            :nodes="_nodes"
            default-expand-all
            node-key="fullPath"
            selected-color="primary"
            :no-selection-unset="true"
            @lazy-load="_treeLazyLoad"
            v-model:selected="_selectedFolder"
          />
        </div>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('cancel')" @click="onDialogCancel" />
        <q-btn
          color="primary"
          :label="_$t('select')"
          @click="_onOKClick"
          :disable="_selectedFolder.length === 0"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, onMounted } from "vue";
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

let _directorySeparatorChar = null;

defineEmits([
  // REQUIRED; need to specify some events that your
  // component will emit through useDialogPluginComponent()
  ...useDialogPluginComponent.emits,
]);

const { dialogRef, onDialogHide, onDialogOK, onDialogCancel } =
  useDialogPluginComponent();

function _onOKClick() {
  const selection = {
    selectedFolder: _selectedFolder.value,
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
  } catch {
    console.error(e);
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredDuringTheOperation"),
      position: "top",
    });

    fail();
  }
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
