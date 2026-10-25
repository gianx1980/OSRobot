<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide">
    <q-card class="q-dialog-plugin" style="width: 900px; max-width: 95vw">
      <q-card-section class="row items-center">
        <div class="text-h6">{{ _$t("variablesAndSecrets") }}</div>
        <q-space />
        <q-btn
          color="primary"
          icon="add"
          :label="_$t('add')"
          @click="_editClick(null)"
        />
      </q-card-section>
      <q-card-section class="q-pt-none text-caption text-grey-8">
        {{ _$t("variablesAndSecretsDescription") }}
      </q-card-section>
      <q-card-section>
        <q-table
          style="height: 400px"
          class="sticky-header-table"
          :columns="_columns"
          :rows="_rows"
          row-key="name"
          dense
          flat
          bordered
          :loading="_loading"
          :pagination="{ rowsPerPage: 0 }"
          hide-pagination
          :no-data-label="_$t('thereAreNoVariables')"
        >
          <template v-slot:body-cell-type="props">
            <q-td :props="props">
              <q-badge
                :color="props.row.isSecret ? 'deep-orange' : 'primary'"
                :label="props.row.isSecret ? _$t('secret') : _$t('variable')"
              />
            </q-td>
          </template>
          <template v-slot:body-cell-value="props">
            <q-td :props="props" class="ellipsis" style="max-width: 250px">
              <span v-if="!props.row.isSecret">{{ props.row.value }}</span>
              <span v-else-if="props.row.isReadable" class="text-grey-7"
                >••••••••</span
              >
              <span v-else class="text-negative">
                <q-icon name="warning" />
                {{ _$t("cannotBeDecrypted") }}
              </span>
            </q-td>
          </template>
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <q-btn
                flat
                dense
                round
                size="sm"
                icon="content_copy"
                @click="_copyClick(props.row)"
                ><q-tooltip>{{ _$t("copyPlaceholder") }}</q-tooltip></q-btn
              >
              <q-btn
                flat
                dense
                round
                size="sm"
                icon="edit"
                @click="_editClick(props.row)"
                ><q-tooltip>{{ _$t("edit") }}</q-tooltip></q-btn
              >
              <q-btn
                flat
                dense
                round
                size="sm"
                icon="delete"
                @click="_deleteClick(props.row)"
                ><q-tooltip>{{ _$t("delete") }}</q-tooltip></q-btn
              >
            </q-td>
          </template>
        </q-table>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn color="primary" :label="_$t('close')" @click="onDialogOK" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent, copyToClipboard } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import Variables, {
  variablePlaceholder,
} from "src/infrastructure/server/Variables.js";
import VariableEditDialog from "src/components/VariableEditDialog.vue";
import Utility from "src/infrastructure/Utility.js";

const _$q = useQuasar();
const _router = useRouter();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

defineEmits([...useDialogPluginComponent.emits]);

const { dialogRef, onDialogHide, onDialogOK } = useDialogPluginComponent();

const _rows = ref([]);
const _loading = ref(false);

const _columns = [
  {
    name: "name",
    label: _$t("name"),
    field: "name",
    align: "left",
    sortable: true,
  },
  { name: "type", label: _$t("type"), field: "isSecret", align: "left" },
  { name: "value", label: _$t("value"), field: "value", align: "left" },
  {
    name: "description",
    label: _$t("description"),
    field: "description",
    align: "left",
  },
  { name: "actions", label: "", field: "name", align: "right" },
];

async function _load() {
  try {
    _loading.value = true;
    const variables = new Variables(_user.token, _user.refreshToken);
    const result = await variables.list();
    _rows.value = result.responseObject ?? [];
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  } finally {
    _loading.value = false;
  }
}

function _editClick(row) {
  _$q
    .dialog({
      component: VariableEditDialog,
      componentProps: { variable: row },
    })
    .onOk(async () => {
      await _load();
    });
}

function _copyClick(row) {
  copyToClipboard(variablePlaceholder(row.name, row.isSecret)).then(() => {
    _$q.notify({
      color: "green",
      message: _$t("placeholderCopied"),
      position: "top",
    });
  });
}

function _deleteClick(row) {
  _$q
    .dialog({
      title: _$t("delete"),
      message: _$t("confirmDeleteVariable", { name: row.name }),
      cancel: true,
      persistent: true,
    })
    .onOk(async () => {
      try {
        const variables = new Variables(_user.token, _user.refreshToken);
        const result = await variables.delete(row.name);
        if (result.responseCode !== Variables.Ok) {
          _$q.notify({
            color: "red",
            message: result.responseMessage ?? _$t("genericError"),
            position: "top",
          });
        }
        await _load();
      } catch (e) {
        Utility.manageException(_$q, _$t, e, _router);
      }
    });
}

onMounted(_load);
</script>
