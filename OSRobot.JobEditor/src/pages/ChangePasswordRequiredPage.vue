<template>
  <q-layout view="hHh Lpr lff">
    <q-page-container>
      <q-page
        class="window-height window-width row justify-center items-center"
      >
        <div class="column">
          <div class="row">
            <h5 class="text-h5 text-white q-my-md">{{ _$t("osRobot") }}</h5>
          </div>
          <div class="row">
            <q-card
              square
              bordered
              class="q-pa-xl shadow-1"
              style="width: 400px"
            >
              <q-card-section>
                <div class="text-body2 q-mb-md">
                  {{ _$t("youMustChangeYourPasswordBeforeContinuing") }}
                </div>
                <q-form class="q-gutter-md" @submit="_submit">
                  <q-input
                    square
                    filled
                    v-model="_formData.currentPassword"
                    :label="_$t('currentPassword')"
                    lazy-rules
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
                  <q-input
                    square
                    filled
                    v-model="_formData.newPassword"
                    :label="_$t('newPassword')"
                    lazy-rules
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
                  <q-input
                    square
                    filled
                    v-model="_formData.confirmPassword"
                    :label="_$t('confirmPassword')"
                    lazy-rules
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
                  <q-btn
                    unelevated
                    color="light-blue-7"
                    size="lg"
                    class="full-width"
                    type="submit"
                    :label="_$t('changePassword')"
                    :loading="_isSubmitting"
                  />
                </q-form>
              </q-card-section>
              <q-card-actions class="q-px-md" align="right">
                <q-btn
                  flat
                  dense
                  :label="_$t('logout')"
                  @click="_router.push('/logout')"
                />
              </q-card-actions>
            </q-card>
          </div>
        </div>
      </q-page>
    </q-page-container>
  </q-layout>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import { Account, User } from "src/infrastructure/server/Account.js";
import Utility from "src/infrastructure/Utility.js";

const _appStore = useAppStore();
const _router = useRouter();
const _i18n = useI18n();
const _$t = _i18n.t;
const _$q = useQuasar();

const _user = _appStore.getLoggedUser();

const _showCurrentPassword = ref(true);
const _showNewPassword = ref(true);
const _showConfirmPassword = ref(true);
const _isSubmitting = ref(false);

const _formData = ref({
  currentPassword: null,
  newPassword: null,
  confirmPassword: null,
});

async function _submit() {
  if (_formData.value.newPassword !== _formData.value.confirmPassword) {
    Utility.showErrorDialog(
      _$q,
      _$t,
      null,
      _$t("theFieldsNewPasswordAndConfirmPasswordDoNotMatch")
    );
    return;
  }

  _isSubmitting.value = true;

  try {
    const account = new Account(_user.token, _user.refreshToken);
    const result = await account.changePassword({
      currentPassword: _formData.value.currentPassword,
      newPassword: _formData.value.newPassword,
      confirmPassword: _formData.value.confirmPassword,
    });

    if (result.responseCode === Account.Ok) {
      // The server issues a fresh token/refreshToken with MustChangePassword cleared
      // specifically so we don't have to force a second login here.
      const updatedUser = new User(
        _user.username,
        result.responseObject.token,
        result.responseObject.refreshToken,
        false
      );
      _appStore.setLoggedUser(updatedUser);

      _$q.notify({
        color: "green",
        message: _$t("passwordChanged"),
        position: "top",
      });
      _router.push("/");
    } else {
      _isSubmitting.value = false;
      switch (result.responseCode) {
        case Account.AccountLockedOut:
          Utility.showErrorDialog(
            _$q,
            _$t,
            null,
            _$t("accountTemporarilyLockedOut")
          );
          break;
        case Account.WrongCredentials:
          Utility.showErrorDialog(
            _$q,
            _$t,
            null,
            _$t("theCurrentPasswordIsIncorrect")
          );
          break;
        default:
          Utility.showErrorDialog(_$q, _$t);
          break;
      }
    }
  } catch (e) {
    _isSubmitting.value = false;
    Utility.showErrorDialog(_$q, _$t);
  }
}
</script>
