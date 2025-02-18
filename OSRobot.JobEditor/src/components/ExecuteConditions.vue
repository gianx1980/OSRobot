<template>
  <q-card class="q-mt-sm q-mb-sm">
    <q-card-section>
      <div class="text-h6">{{ _$t(_conditionType) }}</div>
    </q-card-section>
    <q-card-section>
      <div class="row">
        <div class="col">
          <q-table
            :columns="_columnsDef"
            :rows="_conditionsList"
            row-key="name"
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
                          @click="_conditionEditItemClick(props.row)"
                          >{{ _$t("edit") }}</q-item-section
                        >
                      </q-item>
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_conditionDeleteItemClick(props.row)"
                          >{{ _$t("delete") }}</q-item-section
                        >
                      </q-item>
                      <q-separator />
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_conditionItemMoveUpClick(props.row)"
                          >{{ _$t("moveUp") }}</q-item-section
                        >
                      </q-item>
                      <q-item clickable v-close-popup>
                        <q-item-section
                          @click="_conditionItemMoveDownClick(props.row)"
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
            @click="_conditionAddItemClick"
          />
        </div>
      </div>
    </q-card-section>
  </q-card>
  <q-dialog v-model="_conditionDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 750px; max-width: 80vw; height: 740px"
    >
      <q-card style="width: 700px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _conditionDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_conditionDialogFormSubmit">
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  readonly
                  v-model="_folderItem.label"
                  :label="_$t('object')"
                  dense
                  lazy-rules
                />
              </div>
            </div>
            <div
              class="row"
              v-if="
                _conditionDialogFormData.operator !== 'ObjectExecutes' &&
                _conditionDialogFormData.operator !== 'ObjectDoesNotExecute'
              "
            >
              <div class="col">
                <q-table
                  style="height: 230px; table-layout: fixed"
                  :columns="_dynDataTableColumnsDef"
                  :rows="_dynDataSamplesList"
                  selection="single"
                  v-model:selected="_dynDataSelected"
                  :visible-columns="_dynDataTableColumnsVisibility"
                  :no-data-label="_$t('thereIsNoDynamicDataToShow')"
                  row-key="internalName"
                  dense
                  @row-click="_dynDataTableRowClick"
                ></q-table>
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-select
                  v-model="_conditionDialogFormData.operator"
                  :options="_operators"
                  :label="_$t('operator')"
                  dense
                  lazy-rules
                  map-options
                  emit-value
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>
            <div
              class="row"
              v-if="
                _conditionDialogFormData.operator !== 'ObjectExecutes' &&
                _conditionDialogFormData.operator !== 'ObjectDoesNotExecute'
              "
            >
              <div class="col">
                <q-input
                  filled
                  v-model="_conditionDialogFormData.minValue"
                  :label="
                    _conditionDialogFormData.operator !== 'ValueBetween'
                      ? _$t('value')
                      : _$t('minValue')
                  "
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>
            <div
              class="row"
              v-if="
                _conditionDialogFormData.operator !== 'ObjectExecutes' &&
                _conditionDialogFormData.operator !== 'ObjectDoesNotExecute' &&
                _conditionDialogFormData.operator === 'ValueBetween'
              "
            >
              <div class="col">
                <q-input
                  filled
                  v-model="_conditionDialogFormData.maxValue"
                  :label="_$t('maxValue')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
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
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import Utility from "src/infrastructure/Utility.js";
import { useRouter } from "vue-router";
import { useAppStore } from "src/stores/appStore.js";
import Robot from "src/infrastructure/server/Robot.js";

const _props = defineProps([
  "modelValue",
  "containingFolderItems",
  "conditionType",
]);
const _propsRef = ref(_props);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();
const _router = useRouter();

const _formData = ref(_props);

const _conditionType = _propsRef.value.conditionType;

function _operatorToString(operator) {
  let result = null;
  switch (operator) {
    case "ValueEqualsTo":
      result = _$t("equalsTo").toLowerCase();
      break;
    case "ValueGreaterThan":
      result = _$t("greaterThan").toLowerCase();
      break;
    case "ValueBetween":
      result = _$t("between").toLowerCase();
      break;
    case "ValueLessThan":
      result = _$t("lessThan").toLowerCase();
      break;
    case "ValueContains":
      result = _$t("contains").toLowerCase();
      break;
    case "ValueStartsWith":
      result = _$t("startsWith").toLowerCase();
      break;
    case "ValueEndsWith":
      result = _$t("endsWith").toLowerCase();
      break;
  }

  return result;
}

function _buildConditionDescription(val, row) {
  let conditionDescription = `${_$t("object")} ${_folderItem.label} (${
    _folderItem.id
  })`;

  if (row.operator === "ObjectExecutes") {
    conditionDescription += ` ${_$t("executes")}`;
  } else if (row.operator === "ObjectDoesNotExecute") {
    conditionDescription += ` ${_$t("doesntExecute")}`;
  } else {
    conditionDescription += `, ${_$t("dynamicData").toLowerCase()} ${
      row.dynamicDataCode
    } ${_operatorToString(row.operator)} ${row.minValue}`;

    if (row.operator === "ValueBetween") {
      conditionDescription += ` ${_$t("and").toLowerCase()} ${row.maxValue}`;
    }
  }

  return conditionDescription;
}

const _columnsDef = [
  {
    name: "conditionDescription",
    label: _$t("condition"),
    align: "left",
    field: "dynamicDataCode",
    format: (val, row) => _buildConditionDescription(val, row),
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _conditionsList = ref(_propsRef.value.modelValue[_conditionType]);

function _conditionAddItemClick() {
  const list = _propsRef.value.modelValue[_conditionType];
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _conditionDialogFormData.value = {
    id: maxId,
    dynamicDataCode: null,
    operator: "ObjectExecutes",
    minValue: null,
    maxValue: null,
    isNew: true,
  };

  _dynDataSelected.value = [];
  _conditionDialogTitle.value =
    _conditionType === "executeCondition"
      ? _$t("addExecuteCondition")
      : _$t("addDontExecuteCondition");
  _conditionDialogVisibility.value = true;
}

function _conditionEditItemClick(row) {
  _conditionDialogFormData.value = Object.assign({}, row);
  _conditionDialogTitle.value =
    _conditionType === "executeCondition"
      ? _$t("editExecuteCondition")
      : _$t("editDontExecuteCondition");

  if (row.dynamicDataCode) {
    const dynData = _dynDataSamplesList.value.find(
      (t) => t.internalName === row.dynamicDataCode
    );
    _dynDataSelected.value = [dynData];
  }

  _conditionDialogVisibility.value = true;
}

function _conditionDeleteItemClick(row) {
  const index = _formData.value.modelValue[_conditionType].findIndex(
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
        _formData.value.modelValue[_conditionType].splice(index, 1);
      }
    });
}

function _conditionItemMoveUpClick(row) {
  const list = _formData.value.modelValue[_conditionType];
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _conditionItemMoveDownClick(row) {
  const list = _formData.value.modelValue[_conditionType];
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Dynamic data table
const _dynDataTableColumnsDef = [
  {
    name: "internalName",
    align: "left",
    field: "internalName",
  },
  {
    name: "name",
    label: _$t("name"),
    align: "left",
    field: "name",
  },
  {
    name: "exampleValue",
    label: _$t("exampleValue"),
    align: "left",
    field: "exampleValue",
  },
];

const _dynDataSelected = ref([]);
const _dynDataSamplesList = ref([]);
const _dynDataTableColumnsVisibility = ref(["name", "exampleValue"]);

const _folderItem = _propsRef.value.containingFolderItems.find(
  (folderItem) => folderItem.id === _propsRef.value.modelValue.source
);

function _dynDataTableRowClick(ev, row, index) {
  _dynDataSelected.value = [row];
}

onMounted(async () => {
  try {
    const robot = new Robot(_user.token, _user.refreshToken);

    const dynDataSamplesResponse = await robot.getDynDataSamples(
      _folderItem.pluginId
    );

    _dynDataSamplesList.value = dynDataSamplesResponse.responseObject;
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});

// Dialog management
const _conditionDialogVisibility = ref(false);
const _conditionDialogFormData = ref({});
const _conditionDialogTitle = ref(_$t("addColumn"));

const _operators = [
  { label: _$t("objectExecutes"), value: "ObjectExecutes" },
  { label: _$t("objectDoesNotExecute"), value: "ObjectDoesNotExecute" },
  { label: _$t("valueEqualsTo"), value: "ValueEqualsTo" },
  { label: _$t("valueGreaterThan"), value: "ValueGreaterThan" },
  { label: _$t("valueBetween"), value: "ValueBetween" },
  { label: _$t("valueLessThan"), value: "ValueLessThan" },
  { label: _$t("valueContains"), value: "ValueContains" },
  { label: _$t("valueStartsWith"), value: "ValueStartsWith" },
  { label: _$t("valueEndsWith"), value: "ValueEndsWith" },
];

function _conditionDialogFormSubmit() {
  if (
    _conditionDialogFormData.value.operator !== "ObjectExecutes" &&
    _conditionDialogFormData.value.operator !== "ObjectDoesNotExecute"
  ) {
    _conditionDialogFormData.value.dynamicDataCode =
      _dynDataSelected.value[0].internalName;
  } else {
    _conditionDialogFormData.value.dynamicDataCode = null;
  }

  if (_conditionDialogFormData.value.isNew) {
    _conditionDialogFormData.value.isNew = false;

    _propsRef.value.modelValue[_conditionType].push(
      _conditionDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue[_conditionType].findIndex(
      (i) => i.id === _conditionDialogFormData.value.id
    );

    _propsRef.value.modelValue[_conditionType][index] =
      _conditionDialogFormData.value;
  }

  _conditionDialogVisibility.value = false;
}
</script>
