# 1. Introduction

OS-Robot is a free, open-source automation server. It runs monitoring, integration and automation
**jobs** on the machine where it is installed, and provides a browser-based, drag-and-drop **Job
Editor** to build those jobs visually instead of writing scripts.

Typical uses include nightly database backups with e-mail notification, watching a folder and
processing new files, moving data between files, REST APIs and SQL Server, archiving and
transferring files over FTP/SFTP, and reacting to high CPU, memory or disk usage.

## 1.1 Key concepts

| Concept | Meaning |
|---|---|
| Job | A small flow made of one or more events wired to one or more tasks. Jobs live inside folders. |
| Event | A trigger: a schedule, a file change, high CPU or memory usage, low disk space, a system notification, the service starting. Events start a job; they cannot be the target of a connection. |
| Task | An action: read or write a file, run a program, call a REST API, run a SQL command, send an e-mail, zip a folder, and so on. |
| Connection | An arrow from an event or task to a task. It decides whether, when and how many times the next task runs, using execute / don't-execute conditions. |
| Folder | A container used to organise jobs. Folders can be nested and are shown in the Workspace tree. |
| Dynamic data | Values produced by an object at run time (a file name, a recordset, an HTTP result...) that later tasks can use in their settings, e.g. `{object[3].FileName}`. |
| Recordset | A table of rows produced by a task (for example the rows of a text file or a SQL query). The main one is called `DefaultRecordset`. |
| Iteration | A task can run several times in one execution, typically once per row of a recordset. |

## 1.2 Architecture

OS-Robot is made of two applications:

- **OSRobot.Server** - an ASP.NET Core application that hosts the job engine and the built-in
  plugins, and exposes a JWT-authenticated REST API (with Swagger UI at `/swagger`). It runs as a
  normal process or, on Windows, as a Windows Service.
- **OSRobot Job Editor** - a web application (Vue 3 + Quasar) used from a browser to design jobs,
  run tasks manually and read execution logs. It talks only to the server's API.

The server stores its data in a few places, all relative to the server's folder by default:

| Item | Location | Content |
|---|---|---|
| Jobs | `Data\jobs.json` | The whole workspace: folders, events, tasks and connections. A `.bak` copy of the previous version is kept on each save. |
| Users | SQLite configuration database | The user account, password hash and refresh tokens. Created on first start. |
| Execution logs | `ExecLogs\` | One log per job run, browsable from the Job Editor. |
| Server log | `ExecLogs\osrobot-<date>.log` | Application log (startup, errors, engine activity), one file per day. |
| Audit log | `ExecLogs\audit-*.log` | Logins, lockouts, job saves, configuration reloads and manual task starts, with the user name. |

---

Next: [2. Installation and first start](02-installation.md)
