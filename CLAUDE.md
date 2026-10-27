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
task runs as a **clone** (`CoreHelpers.CloneTaskForRun`: a fresh instance with a JSON-cloned config,
sharing the definition's `ParentFolder` and `Connections`) because the same definition can run
concurrently; successors are taken from the shared definition. `WaitSeconds` uses a timer,
not a worker. `SerialExecution` = 1 worker; otherwise `MaxConcurrentTasks`.

**Failure reporting**: a failed result carries the standard `ErrorMessage` field (set by
`BaseTask.RecordFailure`). After dispatching a task's connections the engine reports each failed
result to the events implementing `ITaskFailureListener` (the Task failure event), with `Handled`
= some connection queued a task for it; errors the engine catches itself are reported too. Those
events are initialized first and publish from their own thread. Runs started by them have
`JobRun.ReportsFailures` false, so a failing report job can't trigger itself in a loop.

**User notifications**: `NotifyUsersTask` calls `Core.UserNotifier` (`IUserNotifier`, no-op by
default), which `JobEngine` installs from its optional constructor parameter. The server's
`HubUserNotifier` broadcasts to every editor connected to `NotificationsHub`
(`/hubs/notifications`, JWT via `access_token` query for that path only; refuses
must-change-password tokens). `Program.cs` forces Serilog's
`Microsoft.AspNetCore.Hosting.Diagnostics` to `Warning` in code, keeping those URLs (and tokens)
out of `osrobot-*.log` whatever `appsettings.json` says. Fire-and-forget: nothing is stored; tests that swap
`Core.UserNotifier` must be `[DoNotParallelize]`.

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
  `[JsonConverter(typeof(JsonStringEnumConverter))]`. Credentials typed into a config are stored in
  `jobs.json` in plain text; users keep them out by writing `{secret['Name']}`, which works only if
  the property is `[DynamicData]` (or resolved by the task itself). Name password properties
  `Password`: saving warns when one holds plain text.
  Config properties need a public getter and setter (get-only collections are silently dropped
  when `jobs.json` loads) and concrete, non-interface types.
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
`{iterationIndex}` or `{subInstanceIndex}`; `{environment['VAR']}`; `{var['Name']}` and
`{secret['Name']}`; and `[CODE]` C# script expressions (Roslyn scripting) — disabled when
`JobEngineConfig:ScriptingEnabled` is false, and they don't see variables or secrets.
All placeholders are replaced in a single regex pass (`PlaceholderRegex`), so a substituted value is
never parsed again: keep it that way, it is what stops external data from expanding a secret.
`DynamicDataValidator` reports broken references, undefined variables/secrets and plain-text
`Password` properties as warnings (on load and on save), not errors.

**Variables and secrets** come from `Core.Variables` (`IVariableResolver`, default the empty
`VariableSet`), installed by `JobEngine` from its optional constructor parameter like
`Core.UserNotifier`. In the server it is `VariableStore` (singleton, table `Variables` in
`config.db`, created by `ConfigDatabaseSchema`): secrets are encrypted with Data Protection
(`DataProtectionSetup`: key ring in `Data/keys`, protected by a certificate, DPAPI or nothing),
served from an immutable snapshot rebuilt after each change, so no engine reload is needed.
At startup `SecretKeyRotation` re-keys and re-encrypts the secrets when the active key's protection
(read from its XML) differs from the configuration: Data Protection never re-encrypts existing keys.
`IVariableResolver.Redact` is applied to every execution-log line (`PluginInstanceLogger`), to
`AppLogger` messages and to Notification task text. `VariablesController` never returns a secret's
value. Tests that swap `Core.Variables` must be `[DoNotParallelize]`; prefer the overloads that take
an `IVariableResolver`.

## Security-relevant settings

`OSRobot.Server/appsettings.json` → `AppSettings:JobEngineConfig` (`ScriptingEnabled`,
`RunProgramAllowedExecutablePaths`, concurrency, log retention) are pushed into the static
`OSRobot.Server.Core.Core` at engine start. `AppSettings:DataProtection` decides how the secrets'
keys are protected; a configured certificate that can't be loaded must stop the server, never
fall back to unprotected keys. The API is loopback-only by default; anyone who can
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
- The XML serializer (`XmlSerialization`/`XmlDeserialization`, `JobsPersistence.SaveXML/LoadXML`)
  is `[Obsolete]` and unused. Configs are cloned (`CoreHelpers.CloneObjects`) by a System.Text.Json
  round-trip with `JsonDeserialization.JobsJsonOptions`, the same options `jobs.json` is loaded with,
  so a cloned config always matches a loaded one. `TestConfigCloning` checks this for every plugin.
