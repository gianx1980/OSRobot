export default class Folder {
  constructor(id, label, icon, header, content, children) {
    this.id = id;
    this.label = label;
    this.icon = icon;
    this.header = header;
    this.content = content || [];
    this.children = children || [];
  }
}
