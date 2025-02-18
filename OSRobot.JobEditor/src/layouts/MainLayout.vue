<template>
  <q-layout view="hHh lpR fFf">
    <q-header bordered class="bg-primary text-white">
      <q-toolbar>
        <q-btn dense flat round icon="menu" @click="_toggleLeftDrawer" />

        <q-toolbar-title>{{ _$t("osRobot") }}</q-toolbar-title>

        <q-btn
          dense
          flat
          round
          icon="sync"
          @click="_forceServerConfigReload"
          :loading="_isReloadingServerConfig"
        >
          <q-tooltip>{{ _$t("forceServersConfigurationReload") }}</q-tooltip>
        </q-btn>

        <q-btn
          dense
          flat
          round
          icon="save"
          @click="_saveClick"
          :loading="_isSaving"
        >
          <q-tooltip>{{ _$t("save") }}</q-tooltip>
        </q-btn>
        <q-btn flat dense icon="person">
          <q-menu>
            <q-list style="min-width: 200px">
              <q-item
                clickable
                v-close-popup
                @click="_showChangePasswordDialog"
              >
                <q-item-section avatar><q-icon name="key" /></q-item-section>
                <q-item-section>{{ _$t("changePassword") }}</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable v-close-popup @click="_exitClick">
                <q-item-section avatar><q-icon name="logout" /></q-item-section>
                <q-item-section>{{ _$t("logout") }}</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </q-toolbar>
    </q-header>

    <q-drawer show-if-above v-model="_leftDrawerOpen" side="left" bordered>
      <div>
        <q-splitter
          v-model="_splitterModel"
          horizontal
          :style="{
            height: _$q.screen.height + 'px',
          }"
        >
          <template v-slot:before>
            <div class="q-pa-md q-gutter-sm">
              <span class="text-h5">{{ _$t("workspaceTree") }}</span>
              <q-tree
                :nodes="_rootFolder"
                dense
                node-key="id"
                selected-color="primary"
                :no-selection-unset="true"
                v-model:selected="_selectedFolder"
              />
            </div>
          </template>

          <template v-slot:after>
            <div class="q-pa-md q-gutter-sm">
              <span class="text-h5">{{ _$t("objectLibrary") }}</span>
              <q-tree
                :nodes="_objectLibTreeNodes"
                dense
                node-key="id"
                selected-color="primary"
                :no-selection-unset="true"
                v-model:selected="_objectLibTreeSelected"
              >
                <template v-slot:header-draggable="prop">
                  <div
                    :id="prop.node.id"
                    :data-label="prop.node.title"
                    :data-type="prop.node.pluginType"
                    class="row items-center"
                    draggable="true"
                    @dragstart="_objectLibTreeNodeStartDrag"
                  >
                    <q-icon :name="prop.node.icon" class="q-mr-sm" />
                    <div>{{ prop.node.title }}</div>
                  </div>
                </template>
              </q-tree>
            </div>
          </template>
        </q-splitter>
      </div>
    </q-drawer>

    <q-drawer v-model="_rightDrawerOpen" side="right" :width="600" bordered>
      <component
        :key="_refConfigFormKey"
        ref="_refConfigForm"
        :is="_selectedItemConfigForm"
        :containingFolderItems="_containingFolderItems"
        v-model="_selectedItemConfig"
        @nodeNeedsUpdate="_nodeNeedsUpdate"
      />
      <!-- drawer content -->
    </q-drawer>

    <q-page-container>
      <div
        id="cont1"
        :style="{ height: $q.screen.height + 'px' }"
        @drop="_drop"
      >
        <VueFlow
          :nodes="_selectedFolderNodes"
          :edges="_selectedFolderEdges"
          :connection-mode="ConnectionMode.Strict"
          @dragover="_vueFlowAllowDrop"
          @nodeClick="_vueFlowNodeClick"
          @edgeClick="_vueFlowEdgeClick"
          @click="_vueFlowClick"
        >
          <template #node-event="eventNodeProps">
            <GenericNode
              v-bind="eventNodeProps"
              @deleteNodeClick="_nodeDelete"
            />
          </template>
          <template #node-task="taskNodeProps">
            <GenericNode
              v-bind="taskNodeProps"
              @deleteNodeClick="_nodeDelete"
              @taskExecute="_taskExecute"
            />
          </template>
          <template #node-folder="folderNodeProps">
            <GenericNode
              v-bind="folderNodeProps"
              @folderOpen="_folderOpen"
              @deleteNodeClick="_nodeDelete"
            />
          </template>
          <template #edge-button="buttonEdgeProps">
            <GenericEdge
              :id="buttonEdgeProps.id"
              :source-x="buttonEdgeProps.sourceX"
              :source-y="buttonEdgeProps.sourceY"
              :target-x="buttonEdgeProps.targetX"
              :target-y="buttonEdgeProps.targetY"
              :source-position="buttonEdgeProps.sourcePosition"
              :target-position="buttonEdgeProps.targetPosition"
              :marker-end="buttonEdgeProps.markerEnd"
              :style="buttonEdgeProps.style"
              @deleteEdgeClick="_edgeDelete"
            />
          </template>
          <Background gap="8"></Background>
        </VueFlow>
      </div>
    </q-page-container>
  </q-layout>
</template>

<script setup>
import EmptyConfigForm from "src/robotObjects/EmptyConfigForm.vue";
import ConnectionConfigForm from "src/robotObjects/connection/ConnectionConfigForm.vue";
import FolderConfigForm from "src/robotObjects/folder/FolderConfigForm.vue";
import DateTimeEventConfigForm from "src/robotObjects/dateTimeEvent/DateTimeEventConfigForm.vue";
import CpuEventConfigForm from "src/robotObjects/cpuEvent/CpuEventConfigForm.vue";
import FileSystemEventConfigForm from "src/robotObjects/fileSystemEvent/FileSystemEventConfigForm.vue";
import DiskSpaceEventConfigForm from "src/robotObjects/diskSpaceEvent/DiskSpaceEventConfigForm.vue";
import MemoryEventConfigForm from "src/robotObjects/memoryEvent/MemoryEventConfigForm.vue";
import OSRobotServiceStartEventConfigForm from "src/robotObjects/osRobotServiceStartEvent/OSRobotServiceStartEventConfigForm.vue";
import SystemEventsEventConfigForm from "src/robotObjects/systemEventsEvent/SystemEventsEventConfigForm.vue";
import ExcelFileTaskConfigForm from "src/robotObjects/excelFileTask/ExcelFileTaskConfigForm.vue";
import FileSystemTaskConfigForm from "src/robotObjects/fileSystemTask/FileSystemTaskConfigForm.vue";
import FtpSftpTaskConfigForm from "src/robotObjects/ftpSftpTask/FtpSftpTaskConfigForm.vue";
import ReadTextFileTaskConfigForm from "src/robotObjects/readTextFileTask/ReadTextFileTaskConfigForm.vue";
import RESTApiTaskConfigForm from "src/robotObjects/restApiTask/RESTApiTaskConfigForm.vue";
import RunProgramTaskConfigForm from "src/robotObjects/runProgramTask/RunProgramTaskConfigForm.vue";
import SendEMailTaskConfigForm from "src/robotObjects/sendEMailTask/SendEMailTaskConfigForm.vue";
import SqlServerBackupTaskConfigForm from "src/robotObjects/sqlServerBackupTask/SqlServerBackupTaskConfigForm.vue";
import SqlServerBulkCopyTaskConfigForm from "src/robotObjects/sqlServerBulkCopyTask/SqlServerBulkCopyTaskConfigForm.vue";
import SqlServerCommandTaskConfigForm from "src/robotObjects/sqlServerCommandTask/SqlServerCommandTaskConfigForm.vue";
import UnzipTaskConfigForm from "src/robotObjects/unzipTask/unzipTaskConfigForm.vue";
import ZipTaskConfigForm from "src/robotObjects/zipTask/zipTaskConfigForm.vue";
import WriteTextFileTaskConfigForm from "src/robotObjects/writeTextFileTask/writeTextFileTaskConfigForm.vue";

import { ref, onMounted, watch } from "vue";
import {
  VueFlow,
  useVueFlow,
  MarkerType,
  Position,
  ConnectionMode,
} from "@vue-flow/core";
const {
  onNodesInitialized,
  updateNode,
  onConnect,
  addEdges,
  vueFlowRef,
  addNodes,
  removeNodes,
  removeEdges,
  getNodes,
  getEdges,
  screenToFlowCoordinate,
} = useVueFlow();
import { Background } from "@vue-flow/background";
//import { Controls } from '@vue-flow/controls'
//import { MiniMap } from '@vue-flow/minimap'
import "@vue-flow/core/dist/style.css"; /* these are necessary styles for vue flow */
import "@vue-flow/core/dist/theme-default.css"; /* this contains the default theme, these are optional styles */
import GenericNode from "./GenericNode.vue";
import GenericEdge from "./GenericEdge.vue";

import { useAppStore } from "../stores/appStore.js";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useQuasar, QSpinnerHourglass } from "quasar";
import Utility from "src/infrastructure/Utility.js";
import Robot from "src/infrastructure/server/Robot.js";
import Workspace from "src/infrastructure/server/Workspace.js";
import RobotObjectUtility from "src/infrastructure/workspace/RobotObjectUtility.js";
import ChangePasswordDialog from "src/components/ChangePasswordDialog.vue";

const _configForms = {
  EmptyConfigForm,
  ConnectionConfigForm,
  FolderConfigForm,
  DateTimeEventConfigForm,
  CpuEventConfigForm,
  FileSystemEventConfigForm,
  DiskSpaceEventConfigForm,
  MemoryEventConfigForm,
  OSRobotServiceStartEventConfigForm,
  SystemEventsEventConfigForm,
  ExcelFileTaskConfigForm,
  FileSystemTaskConfigForm,
  FtpSftpTaskConfigForm,
  ReadTextFileTaskConfigForm,
  RESTApiTaskConfigForm,
  RunProgramTaskConfigForm,
  SendEMailTaskConfigForm,
  SqlServerBackupTaskConfigForm,
  SqlServerBulkCopyTaskConfigForm,
  SqlServerCommandTaskConfigForm,
  UnzipTaskConfigForm,
  ZipTaskConfigForm,
  WriteTextFileTaskConfigForm,
};

const _appStore = useAppStore();
const _router = useRouter();
const _user = _appStore.getLoggedUser();
const _serverConfig = _appStore.getServerConfig();

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();

const _refConfigForm = ref(null);
const _refConfigFormKey = ref(null);

let _isWorkspaceAreaNodeClickEvent = false;

const _isSaving = ref(false);
const _isReloadingServerConfig = ref(false);

const _showReconnectWindowAfterAttempt = 2;
let _failedAttempts = 0;
let _timerId = null;
let _dialog = null;
function _showReconnectWindow() {
  _dialog = Utility.showDialog(
    _$q,
    {
      title: _$t("connectionToServerLost"),
      persistent: true,
      message: _$t("tryingToReconnect"),
      ok: false,
      progress: {
        spinner: QSpinnerHourglass,
        color: "black",
      },
    },
    true
  );
}

function _hideReconnectWindow() {
  if (_dialog) {
    _dialog.hide();
    _dialog = null;
  }
}

// Workspace tree
let _workspaceJobs = null;
const _containingFolderItems = ref([]);
const _rootFolder = ref([]);
const _selectedFolder = ref(null);
const _selectedFolderNodes = ref([]);
const _selectedFolderEdges = ref([]);

// Object library tree
const _robotObjects = ref([]);
const _objectLibTreeSelected = ref("Events");
const _objectLibTreeNodes = ref([
  {
    id: "Events",
    label: _$t("events"),
    icon: "folder",
    children: [],
  },
  {
    id: "Tasks",
    label: _$t("tasks"),
    icon: "folder",
    children: [],
  },
  {
    id: "Folder",
    type: "folder",
    title: _$t("folder"),
    header: "draggable",
    icon: "folder",
  },
]);

const _splitterModel = ref(25);

function _getId() {
  return `${++_workspaceJobs.lastId}`;
}

function _createFlowElement(config, x, y, pluginInfo) {
  let icon = null;
  if (pluginInfo !== null) {
    icon = pluginInfo.icon;
  } else {
    icon = "folder";
  }

  return {
    id: config.id.toString(),
    data: {
      label: config.name,
      icon: icon,
      type: pluginInfo === null ? "folder" : pluginInfo.type,
      supportedOSPlatformList:
        pluginInfo === null ? null : pluginInfo.supportedOSPlatformList,
      pluginTitle: pluginInfo === null ? "Folder" : pluginInfo.title,
    },
    position: { x: x, y: y },
    connectable: pluginInfo === null ? false : true,
    type: pluginInfo === null ? "folder" : pluginInfo.type,
    sourcePosition: Position.Right,
    targetPosition: Position.Left,
    workspaceItemConfig: config,
  };
}

// Drawers status
let _selectedItemConfigForm = "EmptyConfigForm"; //ref("EmptyConfigForm");
let _selectedItemNode = null;
const _selectedItemConfig = ref({});
const _leftDrawerOpen = ref(false);
function _toggleLeftDrawer() {
  _leftDrawerOpen.value = !_leftDrawerOpen.value;
}

const _rightDrawerOpen = ref(false);
function _toggleRightDrawer() {
  _rightDrawerOpen.value = !_rightDrawerOpen.value;
}

function _objectLibTreeNodeStartDrag(ev) {
  let data = {
    pluginId: ev.target.id,
    label: ev.target.dataset.label,
    type: ev.target.dataset.type,
  };

  ev.dataTransfer.setData("droppedPluginInfo", JSON.stringify(data));
}

async function _forceServerConfigReload(ev) {
  try {
    _isReloadingServerConfig.value = true;
    const workspace = new Workspace(_user.token, _user.refreshToken);
    const result = await workspace.reloadJobsConfig();

    let color = null;
    let message = null;
    switch (result.responseCode) {
      case 0:
        color = "green";
        message = _$t("serversConfigurationHasBeenSuccessfullyReloaded");
        break;

      case -3:
        color = "red";
        message = _$t("cannotReloadServersConfigThereAreRunningJobs");
        break;

      default:
        color = "red";
        message = _$t("anErrorOccurredDuringTheOperation");
        break;
    }

    _$q.notify({
      color: color,
      message: message,
      position: "top",
    });
  } catch (e) {
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredDuringTheOperation"),
      position: "top",
    });
  } finally {
    _isReloadingServerConfig.value = false;
  }
}

async function _saveClick(ev) {
  _workspaceJobs[`folder_${_selectedFolder.value}`].edges = getEdges.value;
  _workspaceJobs[`folder_${_selectedFolder.value}`].nodes = getNodes.value;

  try {
    _isSaving.value = true;
    const workspace = new Workspace(_user.token, _user.refreshToken);
    let r = await workspace.save(_workspaceJobs);

    _$q.notify({
      color: "green",
      message: _$t("theJobsHaveBeenSuccessfullySaved"),
      position: "top",
    });
  } catch (e) {
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredDuringTheOperation"),
      position: "top",
    });
  } finally {
    _isSaving.value = false;
  }
}

function _nodeNeedsUpdate(ev) {
  const node = _findTreeNode(_rootFolder.value, ev.nodeId);

  if (node !== null) node.label = ev.newName;

  _selectedItemNode.data.label = ev.newName;
}

function _folderOpen(ev) {
  // Set current folder
  _selectedFolder.value = ev.id;
}

function _nodeDelete(ev) {
  _$q
    .dialog({
      title: _$t("osRobot"),
      message: _$t("doYouWantToDeleteItem"),
      cancel: true,
      persistent: true,
    })
    .onOk(() => {
      // Close the right drawer if user confirm deletion
      _rightDrawerOpen.value = false;

      removeNodes(ev.id);

      // Update folder tree if needed
      if (ev.data.type === "folder") {
        const nodeToDelete = _findTreeNode(_rootFolder.value, ev.id);
        const nodeChildren = _getNodeChildren(nodeToDelete);

        delete _workspaceJobs[`folder_${nodeToDelete.id}`];
        for (let node of nodeChildren) {
          delete _workspaceJobs[`folder_${node.id}`];
        }

        _deleteTreeNode(_rootFolder.value, ev.id);
      }
    });
}

async function _taskExecute(ev) {
  try {
    const workspace = new Workspace(_user.token, _user.refreshToken);
    const result = await workspace.startTask(parseInt(ev.id));

    let color = null;
    let message = null;

    switch (result.responseCode) {
      case 0:
        color = "green";
        message = _$t("taskStartedSuccessfully");
        break;

      default:
        color = "red";
        message = _$t("anErrorOccurredStartingTheTask");
        break;
    }

    _$q.notify({
      color: color,
      message: message,
      position: "top",
    });
  } catch (e) {
    _$q.notify({
      color: "red",
      message: _$t("anErrorOccurredStartingTheTask"),
      position: "top",
    });
  }
}

function _edgeDelete(ev) {
  _$q
    .dialog({
      title: _$t("osRobot"),
      message: _$t("doYouWantToDeleteItem"),
      cancel: true,
      persistent: true,
    })
    .onOk(() => {
      // Close the right drawer if user confirm deletion
      _rightDrawerOpen.value = false;

      removeEdges(ev.id);
    });
}

function _vueFlowAllowDrop(event) {
  event.preventDefault();

  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = "move";
  }
}

function _findTreeNode(nodes, id) {
  for (let i = 0; i < nodes.length; i++) {
    let node = nodes[i];
    if (node.id === id) return node;
    else {
      let nodeFound = _findTreeNode(node.children, id);
      if (nodeFound !== null) return nodeFound;
    }
  }

  return null;
}

function _getNodeChildren(node) {
  let children = [];

  for (let i = 0; i < node.children.length; i++) {
    const childNode = node.children[i];
    children.push(childNode);

    if (childNode.children.length > 0)
      children = children.concat(_getNodeChildren(childNode));
  }

  return children;
}

function _deleteTreeNode(nodes, id) {
  for (let i = 0; i < nodes.length; i++) {
    let node = nodes[i];
    if (node.id === id) {
      nodes.splice(i, 1);
    } else {
      _deleteTreeNode(node.children, id);
    }
  }
}

function _getPluginInfo(pluginId) {
  return _robotObjects.value.filter((v) => v.id === pluginId)[0];
}

function _deepCopy(o) {
  return JSON.parse(JSON.stringify(o));
}

function _drop(ev) {
  ev.preventDefault();
  const data = JSON.parse(ev.dataTransfer.getData("droppedPluginInfo"));

  //const { left, top } = vueFlowRef.value.getBoundingClientRect();

  const newId = _getId();
  let pluginInfo = null;
  let config = null;
  if (data.pluginId !== "Folder") {
    pluginInfo = _getPluginInfo(data.pluginId);

    config = _deepCopy(pluginInfo.configSample); //Object.assign({}, pluginInfo.configSample);
    config.id = newId;
    config.pluginId = data.pluginId;
    config.name = `${pluginInfo.title} ${newId}`;
    config.supportedOSPlatformList = pluginInfo.supportedOSPlatformList;
  } else {
    config = {
      id: newId,
      pluginId: data.pluginId,
      name: `Folder ${newId}`,
    };

    // Update workspace jobs configuration
    _workspaceJobs[`folder_${config.id}`] = {
      id: config.id,
      nodes: [],
      edges: [],
    };

    // Update folder tree
    let currentFolder = _findTreeNode(_rootFolder.value, _selectedFolder.value);

    if (currentFolder !== null) {
      currentFolder.children.push({
        id: config.id,
        icon: "folder",
        label: config.name,
        children: [],
      });
    }
  }

  const position = screenToFlowCoordinate({
    x: ev.clientX,
    y: ev.clientY,
  });

  /**
   * Align node position after drop, so it's centered to the mouse
   *
   * We can hook into events even in a callback, and we can remove the event listener after it's been called.
   */
  const { off } = onNodesInitialized(() => {
    updateNode(config.id, (node) => ({
      position: {
        x: node.position.x - node.dimensions.width / 2,
        y: node.position.y - node.dimensions.height / 2,
      },
    }));

    off();
  });

  const newNode = _createFlowElement(
    config,
    position.x,
    position.y,
    pluginInfo
  );
  addNodes(newNode);
}

function _updateContainingFolderItems() {
  _containingFolderItems.value = _selectedFolderNodes.value.map((t) => {
    return {
      id: t.id,
      type: t.data.type,
      label: t.data.label,
      pluginId: t.workspaceItemConfig.pluginId,
    };
  });
}

function _vueFlowNodeClick(ev) {
  _rightDrawerOpen.value = true;
  _isWorkspaceAreaNodeClickEvent = true; //TODO: stopPropagation doesn't work. Why?
  _selectedItemNode = ev.node;
  _selectedItemConfig.value = ev.node.workspaceItemConfig;

  _updateContainingFolderItems();

  // Needed to force component refresh
  _refConfigFormKey.value = Date.now().toString();
  _selectedItemConfigForm =
    _configForms[`${ev.node.workspaceItemConfig.pluginId}ConfigForm`];
}

function _vueFlowEdgeClick(ev) {
  _rightDrawerOpen.value = true;
  _isWorkspaceAreaNodeClickEvent = true; //TODO: stopPropagation doesn't work. Why?
  _selectedItemNode = null;
  _selectedItemConfig.value = ev.edge.workspaceConnectionConfig;

  _updateContainingFolderItems();

  // Needed to force component refresh
  _refConfigFormKey.value = Date.now().toString();
  _selectedItemConfigForm = _configForms["ConnectionConfigForm"];
}

function _vueFlowClick(ev) {
  if (!_isWorkspaceAreaNodeClickEvent) {
    _rightDrawerOpen.value = false;
    _selectedItemConfigForm = _configForms["EmptyConfigForm"];
  }

  _isWorkspaceAreaNodeClickEvent = false;
}

function _showChangePasswordDialog() {
  _$q
    .dialog({
      component: ChangePasswordDialog,
      componentProps: {
        cancel: true,
        persistent: true,
      },
    })
    .onOk((ev) => {});
}

function _exitClick() {
  _$q
    .dialog({
      title: _serverConfig.appTitle,
      message: _$t("doYouWantToExit"),
      cancel: true,
    })
    .onOk(() => {
      _router.push("/logout");
    });
}

// Watch for changes in the selected folder
// When the folder changes, save the status of the previous selected folder
// and load the status of the new selected folder
watch(_selectedFolder, async (selectedValueCurrent, selectedValuePrev) => {
  if (selectedValuePrev !== null) {
    // Save the status of the previuos folder
    _workspaceJobs[`folder_${selectedValuePrev}`].edges = getEdges.value;
    _workspaceJobs[`folder_${selectedValuePrev}`].nodes = getNodes.value;
  }

  if (selectedValueCurrent !== null) {
    // Load the status of the new selected folder
    _selectedFolderNodes.value =
      _workspaceJobs[`folder_${selectedValueCurrent}`].nodes;
    _selectedFolderEdges.value =
      _workspaceJobs[`folder_${selectedValueCurrent}`].edges;
  }

  _rightDrawerOpen.value = false;
});

onConnect((params) => {
  params.type = "button";
  params.markerEnd = MarkerType.Arrow;

  const sourceInt = params.source;
  const targetInt = params.target;

  // Configure connection and add default exec condition
  params.workspaceConnectionConfig = {
    source: sourceInt,
    target: targetInt,
    enabled: true,
    waitSeconds: 0,
    executeConditions: [
      {
        dynamicDataCode: null,
        operator: "ObjectExecutes",
        minValue: null,
        maxValue: null,
      },
    ],
    dontExecuteConditions: [],
  };

  addEdges(params);
});

onMounted(async () => {
  // If connected flag is false, show the reconnect window immediately
  // (needed if the user press F5)
  if (!_appStore.getConnected() && _dialog == null) {
    _showReconnectWindow();
  }

  /*
  // Heartbeat, token renewal, and connection to server alert management
  const account = new Account(_user.token, _user.refreshToken);
  let hearBeatWorking = false;

  _timerId = setInterval(async () => {
    if (hearBeatWorking) return;
    else hearBeatWorking = true;

    try {
      const result = await account.heartBeat();

      if (result.responseCode === Account.ErrorGeneric) {
        _failedAttempts++;
        if (_failedAttempts < _showReconnectWindowAfterAttempt) return;

        // Generic error, most likely the server is not reachable
        if (_appStore.getConnected() || _dialog == null) {
          _appStore.setConnected(false);
          _showReconnectWindow();
        }
      } else {
        _failedAttempts = 0;

        // Server reachable now check if token is expired or about to expire
        _hideReconnectWindow();
        _appStore.setConnected(true);

        if (Utility.jwtShouldRenew(_user.token)) {
          // It's time to renew the token
          const userRefreshTokenRequest = new UserRefreshTokenRequest(
            _user.token,
            _user.refreshToken
          );

          const resultRefresh = await account.refreshToken(
            userRefreshTokenRequest
          );

          if (resultRefresh.responseCode === Account.Ok) {
            _user.token = resultRefresh.responseObject.token;
            account.token = resultRefresh.responseObject.token;
            _appStore.setLoggedUser(_user);
          } else {
            _router.push("/logout");
          }
        }
      }
    } catch (e) {
      console.error(e);
    } finally {
      hearBeatWorking = false;
    }
  }, _serverConfig.heartBeatInterval);
  */

  try {
    const robot = new Robot(_user.token, _user.refreshToken);

    // Load Tasks and Events from server
    const objectListResponse = await robot.getObjects();
    _robotObjects.value = objectListResponse.responseObject;

    const objectLibEvents = _objectLibTreeNodes.value[0].children;
    const objectLibTasks = _objectLibTreeNodes.value[1].children;

    _robotObjects.value
      .filter((t) => t.type === "event")
      .forEach((t) => {
        t.header = "draggable";
        t.icon = RobotObjectUtility.getPluginIcon(t.id);
        objectLibEvents.push(t);
      });

    _robotObjects.value
      .filter((t) => t.type === "task")
      .forEach((t) => {
        t.header = "draggable";
        t.icon = RobotObjectUtility.getPluginIcon(t.id);
        objectLibTasks.push(t);
      });

    // Load jobs from server
    const getWorkspaceJobsResponse = await robot.getWorkspaceJobs();

    if (getWorkspaceJobsResponse.responseObject !== null) {
      _workspaceJobs = getWorkspaceJobsResponse.responseObject;
      _rootFolder.value = _workspaceJobs.folderTree;

      // The first selected folder will always be 0 (0 = root)
      _selectedFolder.value = "0";
      _selectedFolderNodes.value = _workspaceJobs["folder_0"].nodes;
      _selectedFolderEdges.value = _workspaceJobs["folder_0"].edges;
    } else {
      // No jobs from server, init with an empty tree
      console.error("No jobs from server...");
      _$q.notify({
        color: "red",
        message: _$t("anErrorOccurredDuringTheOperation"),
        position: "top",
      });
    }
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
});
</script>
