<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 1100px; max-width: 1100px">
      <q-card-section>
        <div class="text-h6">{{ _$t("dynamicDataEditor") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-2">
            <b>{{ _$t("mode") }}:</b> {{ _codeType }}
          </div>
          <div class="col-1"></div>
          <div class="col-3">
            <q-btn
              color="primary"
              :label="_$t('browseDynamicData')"
              @click="_browseDynamicDataClick"
            />
          </div>
          <div class="col-3">
            <q-btn color="primary" :label="_$t('insertVariable')">
              <q-menu>
                <q-list style="min-width: 100px">
                  <q-item clickable v-close-popup>
                    <q-item-section
                      @click="_insertVariable(_placeholderIterationIndex)"
                      >iterationIndex</q-item-section
                    >
                  </q-item>
                  <q-item clickable v-close-popup>
                    <q-item-section
                      @click="_insertVariable(_placeholderSubInstanceIndex)"
                      >subInstanceIndex</q-item-section
                    >
                  </q-item>
                </q-list>
              </q-menu>
            </q-btn>
          </div>
          <div
            class="col-12 q-pa-md q-gutter-sm"
            style="height: 500px; overflow: auto"
          >
            <q-input
              ref="_codeArea"
              v-model="_code"
              filled
              type="textarea"
              input-style="height:450px; font-family: monospace; resize: none;"
            />
          </div>
        </div>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('cancel')" @click="onDialogCancel" />
        <q-btn color="primary" :label="_$t('ok')" @click="onOKClick" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>
<script setup>
import { ref, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import DynamicDataBrowserDialog from "src/components/DynamicDataBrowserDialog.vue";
import Robot from "src/infrastructure/server/Robot.js";
import Utility from "src/infrastructure/Utility.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _router = useRouter();

const _props = defineProps(["containingFolderItems", "code"]);

const _code = ref(null);
const _codeType = ref(null);
const _codePrefix = "[CODE]";
const _modeSimple = "Simple";
const _modeCSharp = "C#";

const _placeholderIterationIndex = "iterationIndex";
const _placeholderSubInstanceIndex = "subInstanceIndex";
const _codeArea = ref(null);

watch(_code, (newValue) => {
  if (newValue.substring(0, _codePrefix.length) === _codePrefix)
    _codeType.value = _modeCSharp;
  else _codeType.value = _modeSimple;
});

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
  const output = {
    code: _code.value,
  };

  onDialogOK(output);
}

function _browseDynamicDataClick() {
  _$q
    .dialog({
      component: DynamicDataBrowserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
        containingFolderItems: _props.containingFolderItems,
      },
    })
    .onOk((ev) => {
      let dynamicData;

      if (_codeType.value === _modeSimple) {
        /* Build dynamic data string like this {Object[55].ExecutionStartDateYear} */
        dynamicData = `{object[${ev.objectSelected.id}].${ev.dynDataSelected.internalName}}`;
      } else {
        /* Build C# code like dynamicDataChain[55]["ExecutionStartDateYear"] */
        dynamicData = `dynamicDataChain[${ev.objectSelected.id}]["${ev.dynDataSelected.internalName}"]`;
      }

      _insertTextAtPosition(_codeArea, dynamicData);
    });
}

function _insertVariable(placeholder) {
  let dynamicData;

  if (_codeType.value === _modeSimple) {
    /* Build dynamic data string like this {Object[55].ExecutionStartDateYear} */
    dynamicData = `{${placeholder}}`;
  } else {
    /* Build C# code like dynamicDataChain[55]["ExecutionStartDateYear"] */
    dynamicData = placeholder;
  }

  _insertTextAtPosition(_codeArea, dynamicData);
}

function _insertTextAtPosition(quasarInputRef, text) {
  const nativeInput = quasarInputRef.value.$el.querySelector("textarea");

  const start = nativeInput.selectionStart;
  const end = nativeInput.selectionEnd;
  const before = nativeInput.value.substring(0, start);
  const after = nativeInput.value.substring(end);

  const newText = before + text + after;

  _code.value = newText;

  quasarInputRef.value.focus();
}

onMounted(async () => {
  _codeType.value = _modeSimple;
  _code.value = _props.code;
});
</script>
