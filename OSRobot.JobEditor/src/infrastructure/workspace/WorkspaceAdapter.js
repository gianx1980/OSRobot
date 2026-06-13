import { MarkerType, Position } from "@vue-flow/core";
import RobotObjectUtility from "./RobotObjectUtility.js";

/**
 * Converts a stored JobItem (business model) into a VueFlow node (display model).
 *
 * @param {object} jobItem - Pure business object: { id, pluginId, name, position, ...pluginConfig }
 * @param {object|null} pluginInfo - Plugin metadata from server (null for folders)
 * @returns VueFlow node
 */
export function toFlowNode(jobItem, pluginInfo) {
  const isFolder = pluginInfo === null;
  const icon = isFolder ? "folder" : RobotObjectUtility.getPluginIcon(pluginInfo.id);

  return {
    id: jobItem.id.toString(),
    type: isFolder ? "folder" : pluginInfo.type,
    position: { x: jobItem.position?.x ?? 0, y: jobItem.position?.y ?? 0 },
    sourcePosition: Position.Right,
    targetPosition: Position.Left,
    connectable: !isFolder,
    data: {
      label: jobItem.name,
      icon,
      type: isFolder ? "folder" : pluginInfo.type,
      pluginTitle: isFolder ? "Folder" : pluginInfo.title,
      supportedOSPlatformList: isFolder ? null : pluginInfo.supportedOSPlatformList,
    },
    workspaceItemConfig: jobItem,
  };
}

/**
 * Extracts a JobItem (business model) from a VueFlow node.
 * Captures the current node position so layout is preserved on reload.
 *
 * @param {object} flowNode - VueFlow node (from getNodes.value)
 * @returns JobItem
 */
export function fromFlowNode(flowNode) {
  return {
    ...flowNode.workspaceItemConfig,
    position: { x: flowNode.position.x, y: flowNode.position.y },
  };
}

/**
 * Converts a stored JobConnection (business model) into a VueFlow edge (display model).
 *
 * @param {object} jobConnection - Pure business object: { id, source, target, enabled, ... }
 * @returns VueFlow edge
 */
export function toFlowEdge(jobConnection) {
  return {
    id: jobConnection.id,
    source: jobConnection.source,
    target: jobConnection.target,
    type: "button",
    markerEnd: MarkerType.Arrow,
    workspaceConnectionConfig: jobConnection,
  };
}

/**
 * Extracts a JobConnection (business model) from a VueFlow edge.
 *
 * @param {object} flowEdge - VueFlow edge (from getEdges.value)
 * @returns JobConnection
 */
export function fromFlowEdge(flowEdge) {
  return flowEdge.workspaceConnectionConfig;
}

/**
 * Builds a new default JobConnection between two node ids.
 *
 * @param {string} edgeId - Unique edge id
 * @param {string} source - Source node id
 * @param {string} target - Target node id
 * @returns JobConnection
 */
export function createDefaultConnection(edgeId, source, target) {
  return {
    id: edgeId,
    source,
    target,
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
}
