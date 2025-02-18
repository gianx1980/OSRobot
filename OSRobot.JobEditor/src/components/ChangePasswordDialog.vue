<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 400px; max-width: 400px">
      <q-card-section>
        <div class="text-h6">{{ _$t("changePassword") }}</div>
      </q-card-section>
      <q-card-section>
        <q-form class="q-gutter-md" @submit="_formSubmit">
          <div class="row">
            <div class="col">
              <q-input
                filled
                class="q-pr-xs"
                v-model="_model.currentPassword"
                :label="_$t('currentPassword')"
                lazy-rules
                dense
                :type="_showCurrentPassword ? 'password' : 'text'"
                :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
              >
                <template v-slot:append>
                  <q-icon
                    :name="
                      _showCurrentPassword ? 'visibility_off' : 'visibility'
                    "
                    class="cursor-pointer"
                    @click="_showCurrentPassword = !_showCurrentPassword"
                  />
                </template>
              </q-input>
            </div>
          </div>
          <div class="row">
            <div class="col">
              <q-input
                filled
                class="q-pr-xs"
                v-model="_model.newPassword"
                :label="_$t('newPassword')"
                lazy-rules
                dense
                :type="_showNewPassword ? 'password' : 'text'"
                :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
              >
                <template v-slot:append>
                  <q-icon
                    :name="_showNewPassword ? 'visibility_off' : 'visibility'"
                    class="cursor-pointer"
                    @click="_showNewPassword = !_showNewPassword"
                  />
                </template>
              </q-input>
            </div>
          </div>
          <div class="row">
            <div class="col">
              <q-input
                filled
                class="q-pr-xs"
                v-model="_model.confirmPassword"
                :label="_$t('confirmPassword')"
                lazy-rules
                dense
                :type="_showConfirmPassword ? 'password' : 'text'"
                :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
              >
                <template v-slot:append>
                  <q-icon
                    :name="
                      _showConfirmPassword ? 'visibility_off' : 'visibility'
                    "
                    class="cursor-pointer"
                    @click="_showConfirmPassword = !_showConfirmPassword"
                  />
                </template>
              </q-input>
            </div>
          </div>
          <div class="row" align="right">
            <div class="col">
              <q-btn
                class="q-ml-sm"
                color="primary"
                :label="_$t('cancel')"
                @click="onDialogCancel"
              />
              <q-btn
                class="q-ml-sm"
                type="submit"
                color="primary"
                :label="_$t('confirm')"
              />
            </div>
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref } from "vue";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import { Account } from "src/infrastructure/server/Account.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _showCurrentPassword = ref(true);
const _showNewPassword = ref(true);
const _showConfirmPassword = ref(true);

const _model = ref({
  currentPassword: null,
  newPassword: null,
  confirmPassword: null,
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

async function _formSubmit() {
  const account = new Account(_user.token, _user.refreshToken);
  const result = await account.changePassword({
    currentPassword: _model.value.currentPassword,
    newPassword: _model.value.newPassword,
    confirmPassword: _model.value.confirmPassword,
  });

  if (result.responseCode === 0) {
    _$q.notify({
      color: "green",
      message: _$t("passwordChanged"),
      position: "top",
    });
    onDialogOK();
  } else {
    let message = null;
    switch (result.responseCode) {
      case -2:
        message = _$t("theFieldsNewPasswordAndConfirmPasswordDoNotMatch");
        break;

      case -10:
        message = _$t("theCurrentPasswordIsIncorrect");
        break;

      default:
        message = _$t("genericError");
        break;
    }

    _$q.notify({
      color: "red",
      message: message,
      position: "top",
    });
  }
}
</script>
