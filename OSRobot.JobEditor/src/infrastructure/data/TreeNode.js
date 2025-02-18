export default class TreeNode {
  constructor(id, label, icon, header, workspaceFolder, children) {
    this.id = id;
    this.label = label;
    this.icon = icon;
    this.header = header;
    this.workspaceFolder = workspaceFolder;
    this.children = children || [];
  }
}
