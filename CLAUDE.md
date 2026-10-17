# CLAUDE.md

Guidance for working in this repository. See [README.md](README.md) for the product overview,
[SECURITY.md](SECURITY.md) for the trust model, and [docs/user-manual/](docs/user-manual/README.md)
for end-user behavior of every event and task.

## What this is

OS-Robot is a self-hosted automation server: **events** (triggers) are wired to **tasks** (actions)
through **connections** with conditions, organized in **folders**. A .NET 10 backend runs the jobs
and exposes a JWT-protected REST API; a Vue 3 + Quasar + Vue Flow editor (`OSRobot.JobEditor`)
edits them visually.

## Commands

```bash
# Backend (from repo root)
dotnet build OSRobot.sln
dotnet run --project OSRobot.Server                     # http://localhost:7098, Swagger at /swagger, login admin/admin (forced change)
dotnet test OSRobot.Tests/OSRobot.Tests.csproj
dotnet test OSRobot.Tests --filter "FullyQualifiedName~TestDateTimeEvent"   # single class/method
OSROBOT_INTEGRATION_TESTS=1 dotnet test OSRobot.Tests --filter TestCategory=Integration

# Editor (from OSRobot.JobEditor/)
npm install
quasar dev          # API URL comes from OSRobot.JobEditor/AppConfig.js
npm run lint
npm run format
quasar build
```

Test notes:
- The test project uses **MSTest** (not xUnit, despite the README), with method-level parallelism
  enabled in `OSRobot.Tests/MSTestSettings.cs`, so tests must not share mutable state or file paths.
- `IntegrationTests/*` are skipped (Inconclusive) unless `OSROBOT_INTEGRATION_TESTS=1`.
- `TestPlugins/TestSqlServer*` and `TestSendEMailTask` are **not** guarded: they expect a local
  SQL Server (`(local)`, database `TEST`) / an SMTP server and fail without them. Expect those
  failures on a plain dev machine.

## Solution layout and dependency direction

```
OSRobot.Server.Core  <-  OSRobot.Server.Plugins  <-  OSRobot.Server.JobEngineLib  <-  OSRobot.Server (ASP.NET host)
                                                                            ^
                                                                    OSRobot.Tests
```

- **OSRobot.Server.Core** – plugin contracts (`IPlugin`, `IEvent`, `ITask`, `IEventSink`, configs),
  task base classes, `Folder`, connections/conditions, dynamic data, persistence, logging.
- **OSRobot.Server.Plugins** – every built-in event/task, one folder per plugin. Display strings
  live in `Resource.resx`.
- **OSRobot.Server.JobEngineLib** – `JobEngine` (loads `jobs.json`, starts events, dispatches
  connections), `TaskQueue` (bounded worker pool on a `Channel`), `JobRun`.
- **OSRobot.Server** – controllers, JWT auth, SQLite user DB (EF Core), `JobEngineHostedService`.
  On first run it seeds the DB from `Resources/InitDB.txt` and `Data/jobs.json` from
  `Resources/InitJobs.txt` (embedded resources). Schema upgrades are additive `ALTER TABLE`s in
  `Program.cs` (`_upgradeUserSchema`); there are no EF migrations.
- **OSRobot.Server.JobEngine** – a placeholder console app (`Hello world`); not used.
- The release version is set only in `Directory.Build.props`.

## Core runtime model

**Plugins** are discovered by reflection (`PluginRegistry`): any concrete `IPlugin` with a public
parameterless constructor in a loaded assembly or an `OSRobot*.dll` next to the executable. No
registration step. Plugin `Id` must be unique (duplicate throws) and is the key everywhere: in
`jobs.json` (`pluginId`), the editor icon map, and the editor config-form lookup.

**jobs.json** is the editor's own serialized workspace: a `folderTree` plus one `folder_{id}`
object per folder with VueFlow `nodes` (`workspaceItemConfig`) and `edges`
(`workspaceConnectionConfig`). `JsonDeserialization` turns it into a `Folder` tree by
`PluginRegistry.GetPlugin(pluginId)` + deserializing the node config into the type returned by
`GetPluginDefaultConfig()` (case-insensitive, string→int tolerant). `RobotController.WorkspaceJobs`
validates with the same deserializer before an atomic temp-file + `File.Replace` write (keeps
`jobs.json.bak`). Engine reload is a separate call (`ReloadJobsConfig`).

**Engine flow** (`JobEngine.cs`): `Start()` inits all tasks, opens the queue, then inits enabled
events. An event calls `IEventSink.Publish` from its own thread (timer/watcher callback); the
engine only evaluates connections and enqueues targets — it never runs tasks inline. Each queued
task runs as a **clone** (`CoreHelpers.CloneObjects`, XML round-trip) because the same definition
can run concurrently; successors are taken from the shared definition. `WaitSeconds` uses a timer,
not a worker. `SerialExecution` = 1 worker; otherwise `MaxConcurrentTasks`.

**Connections** (`PluginInstanceConnection`): `DontExecuteConditions` are checked first (any match
blocks), then `ExecuteConditions` (any match runs). `RunMode` is `ForEachResult` (default) or
`OnceWithAllResults` (collected into the source's `IterationResults` recordset, judged by
`CollectedResultRule`). New connection/config properties must default to the old behavior, since
existing `jobs.json` files will lack them.

## Writing a task or event

Pattern (see `OSRobot.Server.Plugins/ZipTask/` as a compact example) — one folder with:
- `XxxPlugin : IPlugin` – `Id`, `Title` (from `Resource`), `PluginType`, `SampleDynamicData`
  (start from `CommonDynamicData.BuildStandardDynamicDataSamples`), `GetInstance()`,
  `GetPluginDefaultConfig()`, `SupportedOSPlatforms`.
- `XxxConfig : ITaskConfig` / `IEventConfig` – must carry `Id`, `Name`, `Enabled`, `Log` and, for
  tasks, `PluginIterationMode`/`IterationObject`/`IterationsCount`. Mark user-editable strings that
  accept `{object[N].Field}` placeholders with `[DynamicData]`; enums use
  `[JsonConverter(typeof(JsonStringEnumConverter))]`. Secrets use `[XmlEncryptField]`.
- `Xxx` task – derive from `MultipleIterationTask` (runs once per iteration;
  `RunMultipleIterationTaskAsync(i)` reads `_iterationTaskConfig`) or `SingleIterationTask`
  (`RunSingleIterationTaskAsync()` reads `_taskConfig`). Do not read dynamic values from `Config`
  directly: the base class clones it and resolves dynamic data per iteration.

Task error-handling contract (`BaseTask`): **signal failure by throwing**; the base class turns it
into exactly one failed `ExecResult` per iteration and logs it. Cancellation is not failure — use
`_cancellationToken` in async I/O and let `OperationCanceledException` propagate. Publish output by
filling `_defaultRecordset` (a fresh `DataTable` per iteration) and/or overriding
`PostTaskSucceded` to add entries to the `DynamicDataSet` (e.g. `CommonDynamicData.DefaultRecordsetName`).

Events implement `IEvent.Init(IEventSink sink)` and call `sink.Publish(...)` per occurrence; stop
everything in `Destroy()`.

A new plugin also needs, in the editor: a config form at
`OSRobot.JobEditor/src/robotObjects/<camelCaseId>/<Id>ConfigForm.vue`, imported and added to the
component map in `src/layouts/MainLayout.vue` (looked up as `` `${pluginId}ConfigForm` ``), an icon
in `src/infrastructure/workspace/RobotObjectUtility.js`, and strings in `src/i18n/en-US/index.js`.
Add tests under `OSRobot.Tests/TestPlugins/` and docs in `docs/user-manual/09-events.md` or
`10-tasks.md`.

## Dynamic data

Placeholders resolved by `DynamicDataParser`:
`{object[ID].Field}`, `{object[ID].Recordset[row]['Column']}` where row is a number,
`{iterationIndex}` or `{subInstanceIndex}`; `{environment['VAR']}`; and `[CODE]` C# script
expressions (Roslyn scripting) — disabled when `JobEngineConfig:ScriptingEnabled` is false.
`DynamicDataValidator` reports broken references as warnings (on load and on save), not errors.

## Security-relevant settings

`OSRobot.Server/appsettings.json` → `AppSettings:JobEngineConfig` (`ScriptingEnabled`,
`RunProgramAllowedExecutablePaths`, concurrency, log retention) are pushed into the static
`OSRobot.Server.Core.Core` at engine start. The API is loopback-only by default; anyone who can
authenticate or write `jobs.json` can run code as the service account. `MustChangePasswordFilter`
blocks all endpoints until the seeded admin password is changed. Read SECURITY.md before changing
auth, scripting, or `RunProgramTask`.

## Conventions

- Every source file starts with the SPDX header (GPL-3.0-or-later):
  ```
  // SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
  // SPDX-License-Identifier: GPL-3.0-or-later
  ```
- C#: file-scoped namespaces matching folders, explicit types rather than `var`, Allman braces,
  4-space indent, CRLF, nullable enabled, collection expressions (`[]`), primary constructors.
  Private fields are `_camelCase`. Comments explain *why* (contracts, threading, compat) — keep
  that density in engine/base-class code.
- Editor: Vue 3 `<script setup>`, Quasar components, ESLint + Prettier; module-level helper
  functions are prefixed `_`.
- `JobsPersistence.SaveXML/LoadXML` are legacy; the XML serializer is still used for config
  cloning, so properties must survive an XML round-trip.
