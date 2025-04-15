<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("message") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("recipients") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_recipientColumnsDef"
                      :rows="_recipientList"
                      :visible-columns="_recipientColumnVisibility"
                      :no-data-label="_$t('thereAreNoItemsToShow')"
                      row-key="id"
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
                                    @click="_recipientEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _recipientDeleteItemClick(props.row)
                                    "
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _recipientItemMoveUpClick(props.row)
                                    "
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _recipientItemMoveDownClick(props.row)
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
                      @click="_recipientAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm q-mb-lg">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("cc") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_CCColumnsDef"
                      :rows="_CCList"
                      :visible-columns="_CCColumnVisibility"
                      :no-data-label="_$t('thereAreNoItemsToShow')"
                      row-key="id"
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
                                    @click="_CCEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_CCDeleteItemClick(props.row)"
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_CCItemMoveUpClick(props.row)"
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_CCItemMoveDownClick(props.row)"
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
                      @click="_CCAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
        <div class="row">
          <div class="col-10">
            <q-input
              filled
              v-model="_propsRef.modelValue.subject"
              :label="_$t('subject')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-2">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="subject"
            />
          </div>
        </div>
        <div class="row">
          <div class="col-10">
            <q-input
              type="textarea"
              filled
              v-model="_propsRef.modelValue.message"
              :label="_$t('message')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-2">
            <BtnDynamicDataBrowser
              class="q-ml-sm"
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="message"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("attachments") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_attachmentColumnsDef"
                      :rows="_attachmentList"
                      :visible-columns="_attachmentColumnVisibility"
                      :no-data-label="_$t('thereAreNoItemsToShow')"
                      row-key="id"
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
                                    @click="_attachmentEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _attachmentDeleteItemClick(props.row)
                                    "
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _attachmentItemMoveUpClick(props.row)
                                    "
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="
                                      _attachmentItemMoveDownClick(props.row)
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
                      @click="_attachmentAddItemClick"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("connection") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.sender"
              :label="_$t('sender')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.smtpServer"
              :label="_$t('smtpServer')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.port"
              :label="_$t('port')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              class="q-ml-sm"
              v-model="_propsRef.modelValue.useSSL"
              :label="_$t('useSSL')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-sm">
          <div class="col">
            <q-toggle
              class="q-ml-sm"
              v-model="_propsRef.modelValue.authenticate"
              :label="_$t('authenticate')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              ref="_ctlUsername"
              filled
              v-model="_propsRef.modelValue.username"
              :label="_$t('username')"
              lazy-rules
              dense
              :disable="!_propsRef.modelValue.authenticate"
              :rules="[
                (val) =>
                  !_propsRef.modelValue.authenticate ||
                  !!val ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              ref="_ctlPassword"
              filled
              v-model="_propsRef.modelValue.password"
              :label="_$t('password')"
              lazy-rules
              dense
              :disable="!_propsRef.modelValue.authenticate"
              :rules="[
                (val) =>
                  !_propsRef.modelValue.authenticate ||
                  !!val ||
                  _$t('thisFieldIsMandatory'),
              ]"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
  <q-dialog v-model="_recipientDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _recipientDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_recipientDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_recipientDialogFormData.recipient"
                  :label="_$t('email')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_recipientDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="recipient"
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
  <q-dialog v-model="_CCDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _CCDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_CCDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_CCDialogFormData.cc"
                  :label="_$t('email')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_CCDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="cc"
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
  <q-dialog v-model="_attachmentDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _attachmentDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_attachmentDialogFormSubmit">
            <div class="row">
              <div class="col-9">
                <q-input
                  filled
                  v-model="_attachmentDialogFormData.attachment"
                  :label="_$t('filePath')"
                  dense
                  lazy-rules
                  :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
                />
              </div>
              <div class="col-3">
                <BtnFileBrowser
                  class="q-ml-sm"
                  v-model="_attachmentDialogFormData"
                  modelValueKey="attachment"
                />
                <BtnDynamicDataBrowser
                  class="q-ml-sm"
                  v-model="_attachmentDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="attachment"
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
import { ref, watch, computed, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import BtnFileBrowser from "src/components/BtnFileBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _ctlUsername = ref(null);
const _ctlPassword = ref(null);

onMounted(async () => {});

const _recipientColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "recipient",
    label: _$t("email"),
    align: "left",
    field: "recipient",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

if (!_propsRef.value.modelValue.hasOwnProperty("_tempRecipients"))
  _propsRef.value.modelValue._tempRecipients = [];

const _recipientColumnVisibility = ref(["recipient", "actions"]);
const _recipientList = ref(_propsRef.value.modelValue._tempRecipients);

function _recipientAddItemClick() {
  const list = _propsRef.value.modelValue._tempRecipients;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _recipientDialogFormData.value = {
    id: maxId,
    recipient: "",
    isNew: true,
  };

  _recipientDialogTitle.value = _$t("addEmail");
  _recipientDialogVisibility.value = true;
}

function _recipientEditItemClick(row) {
  _recipientDialogFormData.value = Object.assign({}, row);
  _recipientDialogTitle.value = _$t("editEmail");
  _recipientDialogVisibility.value = true;
}

function _recipientDeleteItemClick(row) {
  const index = _propsRef.value.modelValue._tempRecipients.findIndex(
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
        _propsRef.value.modelValue._tempRecipients.splice(index, 1);

        _propsRef.value.modelValue.recipients =
          _propsRef.value.modelValue._tempRecipients.map((x) => x.recipient);
      }
    });
}

function _recipientItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue._tempRecipients;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _recipientItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue._tempRecipients;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

watch(
  () => _propsRef.value.modelValue.authenticate,
  (newValue) => {
    if (!newValue) {
      _propsRef.value.modelValue.username = "";
      _propsRef.value.modelValue.password = "";
      _ctlUsername.value.resetValidation();
      _ctlPassword.value.resetValidation();
    }
  }
);

// Dialog management
const _recipientDialogVisibility = ref(false);
const _recipientDialogFormData = ref({});
const _recipientDialogTitle = ref(_$t("addColumn"));

function _recipientDialogFormSubmit() {
  if (_recipientDialogFormData.value.isNew) {
    _recipientDialogFormData.value.isNew = false;
    _propsRef.value.modelValue._tempRecipients.push(
      _recipientDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue._tempRecipients.findIndex(
      (i) => i.id === _recipientDialogFormData.value.id
    );

    _propsRef.value.modelValue._tempRecipients[index] =
      _recipientDialogFormData.value;
  }

  _propsRef.value.modelValue.recipients =
    _propsRef.value.modelValue._tempRecipients.map((x) => x.recipient);

  _recipientDialogVisibility.value = false;
}

// CC Management
const _CCColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "cc",
    label: _$t("email"),
    align: "left",
    field: "cc",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _CCColumnVisibility = ref(["cc", "actions"]);

if (!_propsRef.value.modelValue.hasOwnProperty("_tempCC"))
  _propsRef.value.modelValue._tempCC = [];

const _CCList = ref(_propsRef.value.modelValue._tempCC);

function _CCAddItemClick() {
  const list = _propsRef.value.modelValue._tempCC;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _CCDialogFormData.value = {
    id: maxId,
    recipient: "",
    isNew: true,
  };

  _CCDialogTitle.value = _$t("addEmail");
  _CCDialogVisibility.value = true;
}

function _CCEditItemClick(row) {
  _CCDialogFormData.value = Object.assign({}, row);
  _CCDialogTitle.value = _$t("editEmail");
  _CCDialogVisibility.value = true;
}

function _CCDeleteItemClick(row) {
  const index = _propsRef.value.modelValue._tempCC.findIndex(
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
        _propsRef.value.modelValue._tempCC.splice(index, 1);

        _propsRef.value.modelValue.cc = _propsRef.value.modelValue._tempCC.map(
          (x) => x.recipient
        );
      }
    });
}

function _CCItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue._tempCC;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _CCItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue._tempCC;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Dialog management
const _CCDialogVisibility = ref(false);
const _CCDialogFormData = ref({});
const _CCDialogTitle = ref(_$t("addColumn"));

function _CCDialogFormSubmit() {
  if (_CCDialogFormData.value.isNew) {
    _CCDialogFormData.value.isNew = false;
    _propsRef.value.modelValue._tempCC.push(_CCDialogFormData.value);
  } else {
    const index = _propsRef.value.modelValue._tempCC.findIndex(
      (i) => i.id === _CCDialogFormData.value.id
    );

    _propsRef.value.modelValue._tempCC[index] = _CCDialogFormData.value;
  }

  _propsRef.value.modelValue.cc = _propsRef.value.modelValue._tempCC.map(
    (x) => x.cc
  );

  _CCDialogVisibility.value = false;
}

// Attachments Management
const _attachmentColumnsDef = [
  {
    name: "id",
    field: "id",
  },
  {
    name: "attachment",
    label: _$t("attachment"),
    align: "left",
    field: "attachment",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _attachmentColumnVisibility = ref(["attachment", "actions"]);

if (!_propsRef.value.modelValue.hasOwnProperty("_tempAttachments"))
  _propsRef.value.modelValue._tempAttachments = [];

const _attachmentList = ref(_propsRef.value.modelValue._tempAttachments);

function _attachmentAddItemClick() {
  const list = _propsRef.value.modelValue._tempAttachments;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _attachmentDialogFormData.value = {
    id: maxId,
    attachment: "",
    isNew: true,
  };

  _attachmentDialogTitle.value = _$t("addAttachment");
  _attachmentDialogVisibility.value = true;
}

function _attachmentEditItemClick(row) {
  _attachmentDialogFormData.value = Object.assign({}, row);
  _attachmentDialogTitle.value = _$t("editAttachment");
  _attachmentDialogVisibility.value = true;
}

function _attachmentDeleteItemClick(row) {
  const index = _propsRef.value.modelValue._tempAttachments.findIndex(
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
        _propsRef.value.modelValue._tempAttachments.splice(index, 1);

        _propsRef.value.modelValue.attachments =
          _propsRef.value.modelValue._tempAttachments.map((x) => x.attachment);
      }
    });
}

function _attachmentItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue._tempAttachments;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _attachmentItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue._tempAttachments;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Dialog management
const _attachmentDialogVisibility = ref(false);
const _attachmentDialogFormData = ref({});
const _attachmentDialogTitle = ref(_$t("addAttachment"));

function _attachmentDialogFormSubmit() {
  if (_attachmentDialogFormData.value.isNew) {
    _attachmentDialogFormData.value.isNew = false;
    _propsRef.value.modelValue._tempAttachments.push(
      _attachmentDialogFormData.value
    );
  } else {
    const index = _propsRef.value.modelValue._tempAttachments.findIndex(
      (i) => i.id === _attachmentDialogFormData.value.id
    );

    _propsRef.value.modelValue._tempAttachments[index] =
      _attachmentDialogFormData.value;
  }

  _propsRef.value.modelValue.attachments =
    _propsRef.value.modelValue._tempAttachments.map((x) => x.attachment);

  _attachmentDialogVisibility.value = false;
}
</script>
