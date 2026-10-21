# 5. Building jobs

## 5.1 Organising the workspace with folders

Folders keep related jobs together. Drag **Folder** from the Object library onto the canvas, give
it a name in the configuration panel, and double-click it to open it. Folders can be nested; the
Workspace tree always shows the full hierarchy.

Execution logs are grouped by folder: the Logs panel shows the runs of the folder selected in the
Workspace tree.

## 5.2 Adding events and tasks

1. Select the destination folder in the Workspace tree.
2. Drag an event or a task from the Object library and drop it on the canvas.
3. Click the new card to open its configuration panel.
4. Enter a **Name** (mandatory) and fill in the plugin-specific settings.

Every object receives a unique numeric **Object Id**, shown read-only in the *General* section.
This Id is the number used in dynamic data references such as `{object[12].FileName}`.

## 5.3 Connecting objects

Drag from the output handle of an event or task to the input handle (left side) of a task. A new
connection is created with these defaults:

- **Execute condition:** *Object executes* - the next task runs only if the source succeeded;
- **Wait seconds before execute next object:** 0;
- **Run next object:** once for each result;
- **Enabled:** on.

Click the arrow to change its settings (see [Connections](07-connections.md)). Use the delete button
on the arrow to remove it. Events cannot be the target of a connection; folders cannot be
connected.

> [!TIP]
> To handle failures, draw a second connection from the same task and replace its execute
> condition with *Object does not execute*. For example, send a success e-mail on one branch and an
> alert on the other.

## 5.4 Selecting, copying, moving and deleting

| Action | How |
|---|---|
| Select several objects | Shift + drag a rectangle on the canvas, or Ctrl/Cmd + click each object. |
| Copy | Ctrl/Cmd + C |
| Cut | Ctrl/Cmd + X (asks for confirmation) |
| Paste | Ctrl/Cmd + V - pasted objects get new Ids; connections between copied objects are kept. Folders are copied with all their content. |
| Delete | Delete or Backspace key, or the Delete button on a card (asks for confirmation). |
| Open a folder | Double-click its card, or select it in the Workspace tree. |

> [!NOTE]
> Keyboard shortcuts are ignored while the cursor is inside a text field, so you can safely edit
> text in the configuration panel.

## 5.5 Enabling, disabling and logging

Every event and task has two switches in its *General* section:

- **Enabled** - a disabled event never fires; a disabled task is skipped when a connection would
  start it.
- **Log** - writes the object's activity to the execution log. Turn it off for very frequent,
  uninteresting objects to keep logs small.

Connections also have an **Enabled** switch: a disabled connection is ignored.

## 5.6 Saving and applying changes

Click **Save** to send the workspace to the server. The server first validates the whole
configuration: if it is structurally invalid nothing is written and an error is shown. If it is
valid, `jobs.json` is replaced atomically (the previous version is kept as `jobs.json.bak`).

Saving also checks every dynamic data reference. References that will fail at run time - for
example a reference to an object that does not run before the task - do not block the save, but
the editor shows *"The jobs have been saved, with warnings"* and lists them.

> [!WARNING]
> Saving does not change the jobs the engine is running. Click **Reload server configuration**
> (sync arrows) to make the engine load the saved jobs. The reload is refused while jobs are
> running (*"Cannot reload server's config because there are running jobs"*); retry a moment
> later.

## 5.7 Running a task manually

Click the **Execute** button (lightning bolt) on a task card and confirm. The server starts that
task immediately - as configured on the server, i.e. in the last reloaded configuration - and the
editor shows *"Task started successfully"*. The task's successors run as usual according to their
connections. Manual starts are recorded in the audit log.

> [!NOTE]
> A task started manually has no upstream data: references to dynamic data of objects that
> normally run before it cannot be resolved. Use manual starts mainly for the first task of a job.

---

Previous: [4. The Job Editor at a glance](04-job-editor.md) · Next: [6. Settings common to all objects](06-common-settings.md)
