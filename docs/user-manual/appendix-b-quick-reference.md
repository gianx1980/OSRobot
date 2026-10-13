# Appendix B. Quick reference

## B.1 Mouse and keyboard

| Action | Shortcut |
|---|---|
| Add an object | Drag from Object library to canvas |
| Configure an object or connection | Click it |
| Connect | Drag from output handle to a task's left handle |
| Open folder | Double-click |
| Multi-select | Shift + drag, Ctrl/Cmd + click |
| Copy / Cut / Paste | Ctrl/Cmd + C / X / V |
| Delete | Delete or Backspace |

## B.2 Built-in plugins

| Plugin | Type | Platforms | Main dynamic data |
|---|---|---|---|
| [Date/Time](09-events.md#91-datetime-event) | Event | All | standard |
| [CPU](09-events.md#92-cpu-event) | Event | Windows | `CpuUsagePercentage` |
| [Memory](09-events.md#93-memory-event) | Event | All | `MemoryUsagePercentage` |
| [Disk space](09-events.md#94-disk-space-event) | Event | All | `DiskName`, `DiskSpaceBytes` |
| [File system](09-events.md#95-file-system-event) | Event | All | `FullPathName`, `FileName`, `FileExtension`, `ChangeType` |
| [System events](09-events.md#96-system-events-event) | Event | Windows notifications | `EventCode` |
| [OSRobot service start](09-events.md#97-osrobot-service-start-event) | Event | All | standard |
| [Read text file](10-tasks.md#read-text-file) | Task | All | `DefaultRecordset` |
| [Write text file](10-tasks.md#write-text-file) | Task | All | `DefaultRecordset` |
| [Read binary file](10-tasks.md#read-binary-file) | Task | All | `DefaultRecordset` (`FileContent`...) |
| [Write binary file](10-tasks.md#write-binary-file) | Task | All | standard |
| [File system](10-tasks.md#file-system) | Task | All | `FilePathExists`, `DefaultRecordset` |
| [Excel file](10-tasks.md#excel-file) | Task | All | `DefaultRecordset` |
| [Zip](10-tasks.md#zip) / [Unzip](10-tasks.md#unzip) | Task | All | standard |
| [SQL Server command](10-tasks.md#sql-server-command) | Task | All | `DefaultRecordset` |
| [SQL Server bulk copy](10-tasks.md#sql-server-bulk-copy) | Task | All | standard |
| [SQL Server file loader](10-tasks.md#sql-server-file-loader) | Task | All | `RowsLoaded`, `TableName`, `TableCreated` |
| [SQL Server backup](10-tasks.md#sql-server-backup) | Task | All | `SuccessfulBackupsNumber`, `FailedBackupsNumber` |
| [FTP / SFTP](10-tasks.md#ftp--sftp) | Task | All | standard |
| [REST API](10-tasks.md#rest-api) | Task | All | `HttpResult`, `RawContent`, `JsonPathData`, `DefaultRecordset` |
| [Ping](10-tasks.md#ping) | Task | All | `ThresholdSuccessRate` |
| [Send e-mail](10-tasks.md#send-e-mail) | Task | All | standard |
| [Run program](10-tasks.md#105-run-program) | Task | All | standard |

---

Previous: [Appendix A. Worked examples](appendix-a-examples.md) · [Back to contents](README.md)
