# 11. Execution logs and monitoring

## 11.1 Viewing logs in the editor

1. Select a folder in the Workspace tree.
2. Click **Show logs** (clock icon) in the toolbar.
3. The Logs panel lists the runs of that folder's jobs. Click **Refresh** to update it.
4. Click the magnifier on a row to open the log content.

A log records when the event fired, which tasks ran, their iterations and any error message. Only
objects with **Log** switched on write to it.

## 11.2 Log files on the server

- `ExecLogs\` - execution logs, deleted automatically after `CleanUpLogsOlderThanHours` (24 hours
  by default). Increase the value to keep more history.
- `ExecLogs\osrobot-<date>.log` - the application log: start-up, configuration reloads, engine
  errors.
- `ExecLogs\audit-*.log` - security audit trail: logins and lockouts, job saves, configuration
  reloads, manual task starts.

---

Previous: [10. Tasks reference](10-tasks.md) · Next: [12. Security and deployment](12-security.md)
