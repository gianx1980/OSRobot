<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("diskCheck") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-table
              :columns="_diskColumnsDef"
              :rows="_diskCheckList"
              :visible-columns="_diskColumnsVisibility"
              :no-data-label="_$t('thereAreNoDisksToCheck')"
              row-key="name"
              dense
            >
              <template v-slot:body-cell-actions="props">
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
                          @click="_diskEditItemClick(props.row)"
                          >{{ _$t("edit") }}</q-item-section
                        >
                      </q-item>
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_diskDeleteItemClick(props.row)"
                          >{{ _$t("delete") }}</q-item-section
                        >
                      </q-item>
                      <q-separator />
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_diskItemMoveUpClick(props.row)"
                          >{{ _$t("moveUp") }}</q-item-section
                        >
                      </q-item>
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_diskItemMoveDownClick(props.row)"
                          >{{ _$t("moveDown") }}</q-item-section
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
              @click="_diskAddItemClick"
            />
          </div>
        </div>
        <div class="row q-mt-md">
          <div class="col">
            <label class="q-ml-xs">{{ _$t("checkEvery") }}</label>
          </div>
        </div>
        <div class="row">
          <div class="col-2">
            <q-input
              filled
              v-model="_propsRef.modelValue.checkIntervalSeconds"
              :label="_$t('seconds')"
              lazy-rules
              dense
              type="number"
              min="1"
              max="9999"
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 1 && val <= 9999) ||
                  _$t('mustBeAValueBetweenXAndY', ['1', '9999']),
              ]"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
  <q-dialog v-model="_diskDialogVisibility" persistent>
    <div class="q-pa-md q-gutter-sm">
      <q-card style="width: 400px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _diskDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_diskDialogFormSubmit">
            <q-select
              v-model="_diskDialogFormData.disk"
              :options="_driveList"
              :label="_$t('selectADrive')"
              dense
              lazy-rules
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
            <q-select
              v-model="_diskDialogFormData.operator"
              :options="_operators"
              :label="_$t('selectAnOperator')"
              dense
              emit-value
              map-options
              lazy-rules
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
            <q-input
              filled
              v-model.number="_diskDialogFormData.value"
              :label="_$t('value')"
              type="number"
              dense
              lazy-rules
              min="0"
              max="2147483647"
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val > 0 && val < 2147483647) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '2147483647']),
              ]"
            />
            <q-select
              v-model="_diskDialogFormData.unit"
              :options="_units"
              :label="_$t('selectAUnit')"
              dense
              emit-value
              map-options
              lazy-rules
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
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
import { ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import ClientUtils from "src/infrastructure/server/ClientUtils.js";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();
const _propsRef = ref(_props);

const _diskColumnsDef = [
  {
    name: "disk",
    label: _$t("disk"),
    align: "left",
    field: "disk",
    sortable: true,
  },
  {
    name: "operator",
    label: _$t("operator"),
    align: "left",
    field: "operator",
    format: (val, row) => _operators.filter((v) => v.value === val)[0].label,
    sortable: true,
  },
  {
    name: "value",
    label: _$t("value"),
    align: "left",
    field: "value",
    sortable: true,
  },
  {
    name: "unit",
    label: _$t("unit"),
    align: "left",
    field: "unit",
    format: (val, row) => _units.filter((v) => v.value === val)[0].label,
    sortable: true,
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
    sortable: false,
  },
];

const _diskColumnsVisibility = ref([
  "disk",
  "operator",
  "value",
  "unit",
  "actions",
]);

const _diskCheckList = ref(_propsRef.value.modelValue.diskThresholds);

function _diskAddItemClick() {
  const list = _propsRef.value.modelValue.diskThresholds;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _diskDialogFormData.value = {
    id: maxId,
    disk: "",
    operator: "",
    value: "",
    unit: "",
    isNew: true,
  };

  _diskDialogTitle.value = _$t("addDiskCheck");
  _diskDialogVisibility.value = true;
}

function _diskEditItemClick(row) {
  _diskDialogFormData.value = Object.assign({}, row);
  _diskDialogTitle.value = _$t("editDiskCheck");
  _diskDialogVisibility.value = true;
}

function _diskDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.diskThresholds.findIndex(
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
        _propsRef.value.modelValue.diskThresholds.splice(index, 1);
      }
    });
}

function _diskItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.diskThresholds;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _diskItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.diskThresholds;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

onMounted(async () => {
  try {
    const clientUtils = new ClientUtils(_user.token, _user.refreshToken);

    _driveList.value = [];
    const driveListResponse = await clientUtils.getDriveList();
    for (const drive of driveListResponse.responseObject) {
      _driveList.value.push(drive.name);
    }
  } catch (e) {
    console.error(e);
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredDuringTheOperation"),
      position: "top",
    });
  }
});

// Dialog management
const _diskDialogVisibility = ref(false);
const _diskDialogFormData = ref({});
const _diskDialogTitle = ref(_$t("addDiskCheck"));
const _driveList = ref([]);
const _operators = [
  { label: _$t("greaterThan"), value: "GreaterThan" },
  { label: _$t("lessThan"), value: "LessThan" },
];
const _units = [
  {
    label: _$t("megabytesMb"),
    value: "Megabytes",
  },
  {
    label: _$t("gigabytesGb"),
    value: "Gigabytes",
  },
  {
    label: _$t("terabytesTb"),
    value: "Terabytes",
  },
  {
    label: _$t("percentagePt"),
    value: "Percentage",
  },
];

function _diskDialogFormSubmit() {
  if (_diskDialogFormData.value.isNew) {
    _diskDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.diskThresholds.push(_diskDialogFormData.value);
  } else {
    const index = _propsRef.value.modelValue.diskThresholds.findIndex(
      (i) => i.id === _diskDialogFormData.value.id
    );

    _propsRef.value.modelValue.diskThresholds[index] =
      _diskDialogFormData.value;
  }

  _diskDialogVisibility.value = false;
}
</script>
