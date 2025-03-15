<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 1100px; max-width: 1100px">
      <q-card-section>
        <div class="text-h6">{{ _folderName }} - {{ _props.logFileName }}</div>
      </q-card-section>
      <q-card-section>
        <div class="q-pa-md q-gutter-sm" style="height: 500px; overflow: auto">
          <q-input
            v-model="_logContent"
            filled
            type="textarea"
            input-style="height:450px; font-family: monospace; resize: none;"
            :readonly="true"
          />
        </div>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('close')" @click="onDialogCancel" />
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
import Robot from "src/infrastructure/server/Robot.js";
import Utility from "src/infrastructure/Utility.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _router = useRouter();

const _props = defineProps(["folderId", "logFileName"]);

const _logContent = ref(null);
const _folderName = ref(null);
const _folderLogPath = ref(null);

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

onMounted(async () => {
  try {
    const robot = new Robot(_user.token, _user.refreshToken);

    const folderInfoResponse = await robot.getFolderInfo(_props.folderId);
    _folderName.value = folderInfoResponse.responseObject.name;
    _folderLogPath.value = folderInfoResponse.responseObject.logPath;

    console.log(_folderName.value);

    const logContentResponse = await robot.getLogContent(
      _props.folderId,
      _props.logFileName
    );
    _logContent.value = logContentResponse.responseObject;
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});
</script>
