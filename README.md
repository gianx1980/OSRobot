# OS-Robot

OS-Robot is a free, open-source, cross-platform automation server. It runs monitoring,
integration, and automation jobs on your own servers or PC, and gives you a browser-based,
drag-and-drop editor to build them visually instead of writing scripts.

A **job** is a small flow: one or more **events** (triggers - a schedule, a file change, high CPU
usage, a service start, ...) wired to one or more **tasks** (an action - write a file, run a
program, send an email, back up a database, ...), with conditions controlling which tasks run and
when. Jobs are organized into folders and run entirely on the machine OS-Robot is installed on.

## Features

- **Visual job editor** - a canvas ([Vue Flow](https://vueflow.dev/)-based) where you drag events
  and tasks onto the workspace and connect them. Multi-select with Shift-drag or Ctrl/Cmd-click,
  cut/copy/paste (including whole folders, recursively), delete with the Delete key, per-connection
  execution conditions (e.g. "only if the previous task succeeded", or a dynamic-data condition),
  and per-object enable/disable and logging toggles.
- **Events** - `DateTimeEvent` (schedule/recurrence), `CpuEvent`, `MemoryEvent`, `DiskSpaceEvent`,
  `FileSystemEvent`, `SystemEventsEvent`, `OSRobotServiceStartEvent`.
- **Tasks** - file I/O (`ReadTextFileTask`, `WriteTextFileTask`, `ReadBinaryFileTask`,
  `WriteBinaryFileTask`, `FileSystemTask`), archives (`ZipTask`, `UnzipTask`), data
  (`ExcelFileTask`, `SqlServerBackupTask`, `SqlServerBulkCopyTask`, `SqlServerCommandTask`),
  network (`FtpSftpTask`, `RESTAPITask`, `PingTask`), messaging (`SendEMailTask`), and
  `RunProgramTask` for anything else.
- **REST API** (ASP.NET Core, JWT-authenticated, Swagger UI included) that the job editor talks to
  - and that you can drive yourself for automation or a custom front end.
- **Execution logging** per job/task run, browsable from the editor.
- Runs as a normal process or, on Windows, as a Windows Service.

## Repository layout

This repo contains two apps: a .NET backend (API + job engine + plugins) and a Vue/Quasar
frontend (the visual editor), plus a test suite.

| Project | What it is |
|---|---|
| `OSRobot.Server` | ASP.NET Core Web API - authentication, the REST endpoints the job editor uses, and the host that starts/stops the job engine. |
| `OSRobot.Server.Core` | Domain model and persistence: plugin contracts, folder/job structures, `jobs.json` load/save. |
| `OSRobot.Server.JobEngineLib` | The engine itself - schedules events, evaluates connection conditions, executes tasks. |
| `OSRobot.Server.Plugins` | The built-in event and task plugins listed above. |
| `OSRobot.Tests` | xUnit test suite covering the engine and plugins. |
| `OSRobot.JobEditor` | The browser-based visual editor (Vue 3 + Quasar + Vue Flow) that talks to `OSRobot.Server`'s API. |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) for the server.
- [Node.js](https://nodejs.org/) (see `OSRobot.JobEditor/package.json` for supported versions) and
  npm or yarn for the job editor.

### Run the server

```bash
cd OSRobot.Server
dotnet run
```

On first run it creates its own SQLite config database and an empty `Data/jobs.json`. By default
it listens on `http://localhost:7098` (loopback only, safe out of the box) and serves a Swagger
UI at `/swagger`. Log in with the seeded default account, **admin / admin** - the API forces a
password change on first login and will not let you do anything else until you set a new one.

Server settings (listen address, data/log paths, JWT, lockout policy, ...) live in
`OSRobot.Server/appsettings.json`.

### Run the job editor

```bash
cd OSRobot.JobEditor
npm install
quasar dev
```

This starts the editor in dev mode against the running server. See
[OSRobot.JobEditor/README.md](OSRobot.JobEditor/README.md) for linting, formatting, and
production build commands.

### Run the tests

```bash
dotnet test OSRobot.Tests/OSRobot.Tests.csproj
```

## Security and deployment

Anyone who can authenticate to the API - or write to `jobs.json` on disk - can run arbitrary code
as the account OS-Robot runs under. Read [SECURITY.md](SECURITY.md) for the full trust model
before exposing an instance beyond your own machine.

If you need OS-Robot reachable from other machines (LAN or the internet), see
[OSRobot.Server/DEPLOYMENT.md](OSRobot.Server/DEPLOYMENT.md) for HTTPS setup with IIS,
nginx/Caddy, or direct Kestrel.

## License

OS-Robot is licensed under the [GNU General Public License v3.0](LICENSE).

## Links

- Website: [os-robot.com](https://www.os-robot.com)
