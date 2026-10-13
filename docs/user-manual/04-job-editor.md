# 4. The Job Editor at a glance

After login the editor shows a single workspace made of a toolbar, a left panel, a central canvas
and panels that open on the right.

## 4.1 Toolbar

| Button (icon) | Function |
|---|---|
| Menu (☰) | Shows or hides the left panel. |
| Show logs (clock) | Opens the Logs panel for the folder currently selected. |
| Reload server configuration (sync arrows) | Tells the job engine to reload `jobs.json`, so saved changes take effect. Refused while jobs are running. |
| Save (disk) | Saves the whole workspace to the server. |
| User (person) | Menu with **Change password**, **About** and **Logout**. |

## 4.2 Left panel

- **Workspace tree** - the folder hierarchy. Click a folder to show its content on the canvas.
- **Object library** - the list of **Events**, **Tasks** and the **Folder** object. Drag an item
  onto the canvas to add it.

## 4.3 Canvas

The canvas shows the objects of the selected folder as cards, and the connections between them as
arrows. Each card shows:

- the plugin icon (hover it to see the object type);
- a computer icon - hover it to see the operating systems the plugin supports;
- an **Execute** button (lightning bolt), on tasks only, to start the task manually;
- a **Delete** button;
- the object's name.

Tasks have an input handle on their left side; events and tasks have an output handle used to
draw connections. Double-click a folder card to open it.

## 4.4 Configuration panel

Click an object or a connection to open its configuration panel on the right. Changes are kept in
the editor immediately but are written to the server only when you click **Save** (see
[Saving and applying changes](05-building-jobs.md#56-saving-and-applying-changes)).

---

Previous: [3. Server configuration](03-configuration.md) · Next: [5. Building jobs](05-building-jobs.md)
