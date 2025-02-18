<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <SqlServerConnectionConfigForm
      v-model="_propsRef.modelValue"
      showDatabaseField="true"
      :testConnectionButtonLabel="_$t('testConnection')"
      @attemptedConnection="_attemptedConnection"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("command") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-11">
            <q-input
              type="number"
              filled
              v-model="_propsRef.modelValue.commandTimeout"
              :label="_$t('commandTimeoutSeconds')"
              lazy-rules
              dense
              :rules="[
                (val) => !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val > 0 && val < 2147483647) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '2147483647']),
              ]"
            />
            <div class="col-1"></div>
          </div>
        </div>
        <div class="row">
          <div class="col-11">
            <q-input
              filled
              type="textarea"
              v-model="_propsRef.modelValue.query"
              :label="_$t('query')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="query"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.type"
              :options="_commandTypes"
              :label="_$t('type')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm q-mb-sm">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("parameters") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_parameterColumnsDef"
                      :rows="_parameterList"
                      :visible-columns="_parameterColumnVisibility"
                      :no-data-label="_$t('thereAreNoItemsToShow')"
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
                                    @click="_parameterEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _parameterDeleteItemClick(props.row)
                                    "
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _parameterItemMoveUpClick(props.row)
                                    "
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _parameterItemMoveDownClick(props.row)
                                    "
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
                      @click="_parameterAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-toggle
              right-label
              v-model="_propsRef.modelValue.returnsRecordset"
              :label="_$t('returnsRecordset')"
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
  <q-dialog v-model="_parameterDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card class="q-px-sm q-pb-md" style="width: 600px">
        <q-card-section>
          <div class="text-h6">{{ _parameterDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_parameterDialogFormSubmit">
            <div class="row q-mb-xs">
              <div class="col">
                <q-select
                  v-model="_parameterDialogFormData.type"
                  :options="_parameterTypes"
                  :label="_$t('type')"
                  dense
                  lazy-rules
                  map-options
                  emit-value
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_parameterDialogFormData.name"
                  :label="_$t('name')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_parameterDialogFormData.value"
                  :label="_$t('value')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_parameterDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="value"
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-toggle
                  right-label
                  v-model="_parameterDialogFormData.passNullValue"
                  :label="_$t('passNullValue')"
                  dense
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_parameterDialogFormData.length"
                  :label="_$t('lengthPrecision')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                  :disable="
                    !['Varchar', 'NVarchar', 'Numeric'].includes(
                      _parameterDialogFormData.type
                    )
                  "
                />
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  filled
                  v-model="_parameterDialogFormData.scale"
                  :label="_$t('scale')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                  :disable="
                    !['Numeric'].includes(_parameterDialogFormData.type)
                  "
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
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import SqlServerConnectionConfigForm from "src/components/SqlServerConnectionConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

async function _attemptedConnection(eventArgs) {
  const message = eventArgs.result
    ? _$t("connectionSucceeded")
    : _$t("cantConnectToSqlServer");

  _$q.dialog({
    title: _$t("osRobot"),
    message: message,
    cancel: false,
    persistent: true,
  });
}

const _commandTypes = [
  { label: _$t("text"), value: "Text" },
  { label: _$t("storedProcedure"), value: "StoredProcedure" },
];

const _parameterColumnsDef = [
  {
    name: "name",
    label: _$t("name"),
    align: "left",
    field: "name",
  },
  {
    name: "value",
    label: _$t("value"),
    align: "left",
    field: "value",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _parameterColumnVisibility = ref(["name", "value", "actions"]);

const _parameterList = ref(_propsRef.value.modelValue.paramsDefinition);

function _parameterAddItemClick() {
  const list = _propsRef.value.modelValue.paramsDefinition;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _parameterDialogFormData.value = {
    id: maxId,
    name: null,
    value: null,
    passNullValue: false,
    type: "Varchar",
    length: null,
    precision: null,
    isNew: true,
  };

  _parameterDialogTitle.value = _$t("addParameter");
  _parameterDialogVisibility.value = true;
}

function _parameterEditItemClick(row) {
  _parameterDialogFormData.value = Object.assign({}, row);
  _parameterDialogTitle.value = _$t("editParameter");
  _parameterDialogVisibility.value = true;
}

function _parameterDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.paramsDefinition.findIndex(
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
        _propsRef.value.modelValue.paramsDefinition.splice(index, 1);
      }
    });
}

function _parameterItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.paramsDefinition;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _parameterItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.paramsDefinition;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Dialog management
const _parameterDialogVisibility = ref(false);
const _parameterDialogFormData = ref({});
const _parameterDialogTitle = ref(_$t("addColumn"));

function _parameterDialogFormSubmit() {
  if (_parameterDialogFormData.value.isNew) {
    _parameterDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.paramsDefinition.push(
      _parameterDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue.paramsDefinition.findIndex(
      (i) => i.id === _parameterDialogFormData.value.id
    );

    _propsRef.value.modelValue.paramsDefinition[index] =
      _parameterDialogFormData.value;
  }

  _parameterDialogVisibility.value = false;
}

const _parameterTypes = [
  { label: _$t("varchar"), value: "Varchar" },
  { label: _$t("nVarchar"), value: "NVarchar" },
  { label: _$t("xml"), value: "Xml" },
  { label: _$t("numeric"), value: "Numeric" },
  { label: _$t("int"), value: "Int" },
  { label: _$t("long"), value: "Long" },
  { label: _$t("date"), value: "Date" },
  { label: _$t("dateTime"), value: "Datetime" },
  { label: _$t("bit"), value: "Bit" },
];
</script>
