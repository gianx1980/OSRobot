# Browser Notifications ("Notify users" task) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new "Notify users" task that sends a one-way message to every open OS-Robot editor, shown as a system notification when the tab is in the background and in-app when it is in front.

**Architecture:** Core defines `IUserNotifier` (no-op by default) exposed as `Core.UserNotifier`; `JobEngine` installs the one it is given. The server implements it with a SignalR hub (`/hubs/notifications`, JWT-authenticated) broadcasting to all clients. The editor connects with `@microsoft/signalr` while `MainLayout` is mounted and presents each message with Quasar `Notify` or the browser `Notification` API.

**Tech Stack:** .NET 10, ASP.NET Core SignalR (shared framework), MSTest 4; Vue 3 + Quasar 2, `@microsoft/signalr`.

**Spec:** `docs/superpowers/specs/2026-10-20-browser-notifications-design.md`

## Global Constraints

- **Do not commit.** Each task ends with its changes left in the working tree for the user to review; the user commits.
- SPDX header on every new source file (C#: `//` lines; Vue: `<!-- -->` lines), exactly as in CLAUDE.md.
- C#: file-scoped namespaces matching folders, explicit types (no `var`), Allman braces, 4-space indent, nullable enabled, `[]` collection expressions, `_camelCase` private fields.
- Config properties have public getters **and** setters; enums carry `[JsonConverter(typeof(JsonStringEnumConverter))]`.
- Title capped at **200** characters, message at **2,000**.
- Levels: `Info` (default), `Success`, `Warning`, `Error`.
- Hub path: `/hubs/notifications`; client method name: `"notification"`; payload `{ title, message, level, timestamp, sourceName }` with `level` as a string.
- `access_token` query parameter is accepted **only** for paths starting with `/hubs/notifications`.
- In-app notifications: `Error` stays until dismissed, others close after **8 seconds**; text is never rendered as HTML.
- Mute only silences system notifications; per-browser flag in `localStorage`.
- Editor helper functions at module level are prefixed `_`; `<script setup>`; run `npm run lint` clean.
- Tests touching `Core.UserNotifier` are `[DoNotParallelize]` and restore the no-op notifier afterwards.

## Review Focus

1. **Token renewal on reconnect after a long idle tab** — a tab open for hours must reconnect with a fresh token, not loop on 401. Covered manually in Task 6 (leave tab idle past token lifetime, restart server).
2. **Placeholder that resolves to a huge value** (a whole recordset in the message) — must be truncated, not broadcast. Test in Task 2 (`Long_title_and_message_are_truncated`).
3. **Must-change-password user** — must not receive notifications even though their JWT is valid. Test in Task 3 (`OnConnected_aborts_when_password_change_is_required`) + manual in Task 6.
4. **Placeholder that resolves to an empty title** (e.g. empty upstream field) — that iteration fails visibly instead of sending a blank notification; the other iterations still run (base-class contract). Test in Task 2 (`Blank_title_fails_the_iteration`).
5. **Editor opened over plain HTTP from another machine** — no exception; the menu item is disabled with the "requires HTTPS or localhost" reason and in-app notifications still work. Manual in Task 6.

---

### Task 1: Notification contract in Core and engine wiring

**Files:**
- Create: `OSRobot.Server.Core/Notifications/IUserNotifier.cs`
- Create: `OSRobot.Server.Core/Notifications/UserNotification.cs` (record + `UserNotificationLevel` enum)
- Create: `OSRobot.Server.Core/Notifications/NullUserNotifier.cs`
- Modify: `OSRobot.Server.Core/Core.cs` (`Init`, new `UserNotifier` property)
- Modify: `OSRobot.Server.JobEngineLib/JobEngine.cs` (primary constructor, `Start()` call to `Core.Init` at ~line 424)
- Create: `OSRobot.Tests/Support/RecordingUserNotifier.cs`
- Test: `OSRobot.Tests/TestClasses/TestUserNotifierWiring.cs`

**Interfaces:**
- Produces (namespace `OSRobot.Server.Core.Notifications`):
  - `public enum UserNotificationLevel { Info, Success, Warning, Error }`
  - `public sealed record UserNotification(string Title, string Message, UserNotificationLevel Level, DateTime Timestamp, string SourceName);`
  - `public interface IUserNotifier { Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken); }`
  - `public sealed class NullUserNotifier : IUserNotifier` with `public static NullUserNotifier Instance { get; }`
- Produces in `OSRobot.Server.Core.Core`: `public static IUserNotifier UserNotifier { get; private set; }` (initially `NullUserNotifier.Instance`); `Init(string logPath, bool scriptingEnabled = true, string? runProgramAllowedExecutablePaths = null, IUserNotifier? userNotifier = null)` — null installs `NullUserNotifier.Instance`.
- Produces: `JobEngine(IAppLogger appLogger, IJobEngineConfig config, IUserNotifier? userNotifier = null)`; `Start()` passes it to `Core.Init`.
- Produces (tests, namespace `OSRobot.Tests.Support`): `public sealed class RecordingUserNotifier : IUserNotifier` with `IReadOnlyList<UserNotification> Notifications` (thread-safe) and `Func<UserNotification, Exception?>? FailWith` (when set and it returns non-null, `NotifyAsync` throws that exception).

- [ ] **Step 1: Write the failing tests** in `TestUserNotifierWiring` (`[TestClass]`, `[DoNotParallelize]`, `[TestCleanup]` calls `Core.Init(PluginInstanceLogger.LogPath)`):
  - `Init_without_notifier_installs_the_no_op` — after `Core.Init(PluginInstanceLogger.LogPath)`, `Assert.AreSame(NullUserNotifier.Instance, Core.UserNotifier)`.
  - `Init_installs_the_given_notifier` — `Core.Init(PluginInstanceLogger.LogPath, userNotifier: fake)` → `Assert.AreSame(fake, Core.UserNotifier)`.
  - `Engine_start_installs_its_notifier` — build `JobEngine` the way `EngineHarness` does (temp folder, `new JobGraph().ToJson()`), passing a `RecordingUserNotifier`; after `Start()`, `Assert.AreSame(fake, Core.UserNotifier)`; `Stop()`, restore `PluginInstanceLogger.LogPath`, delete the folder. (Add an optional `IUserNotifier? userNotifier = null` parameter to `EngineHarness`'s constructor and reuse it rather than duplicating setup.)
- [ ] **Step 2: Run** `dotnet test OSRobot.Tests --filter "FullyQualifiedName~TestUserNotifierWiring"` — expected: build FAILS (types don't exist).
- [ ] **Step 3: Implement** the types and changes listed under Interfaces. XML doc comments: `IUserNotifier` states that notifications are fire-and-forget to whoever is listening, and that "nobody listening" is not an error; `Core.UserNotifier` follows the existing "process-wide state set by JobEngine.Start()" comment style.
- [ ] **Step 4: Run** the same filter — expected: 3 passed. Then the full suite `dotnet test OSRobot.Tests/OSRobot.Tests.csproj` — expected: no new failures.
- [ ] **Step 5: Hand off for review** (do not commit).

---

### Task 2: NotifyUsersTask plugin

**Files:**
- Create: `OSRobot.Server.Plugins/NotifyUsersTask/NotifyUsersTaskPlugin.cs`
- Create: `OSRobot.Server.Plugins/NotifyUsersTask/NotifyUsersTaskConfig.cs`
- Create: `OSRobot.Server.Plugins/NotifyUsersTask/NotifyUsersTask.cs`
- Modify: `OSRobot.Server.Plugins/Resource.resx` + `Resource.Designer.cs` (new `TxtNotifyUsersTask` = "Notify users", alphabetical position, same shape as `TxtPingTask`)
- Modify: `OSRobot.Tests/TestClasses/TestPluginRegistry.cs` (built-in count `24` → `25`)
- Test: `OSRobot.Tests/TestPlugins/TestNotifyUsersTask.cs`

**Interfaces:**
- Consumes: `Core.UserNotifier`, `UserNotification`, `UserNotificationLevel`, `RecordingUserNotifier` (Task 1).
- Produces: plugin `Id = "NotifyUsersTask"`, `Title = Resource.TxtNotifyUsersTask`, `PluginType = EnumPluginType.Task`, `SampleDynamicData = CommonDynamicData.BuildStandardDynamicDataSamples("Notify users 1")`, `SupportedOSPlatforms = EnumOSPlatform.All`.
- Produces `NotifyUsersTaskConfig : ITaskConfig` (JSON names as the editor will send them: `title`, `message`, `level`):
  `Id`, `Name`, `Enabled = true`, `Log = true`, `[DynamicData] Title = ""`, `[DynamicData] Message = ""`, `Level = UserNotificationLevel.Info`, `PluginIterationMode = IterationMode.IterateExactNumber`, `IterationObject = ""`, `IterationsCount = 1` (enums with `JsonStringEnumConverter`).
- Produces `public class NotifyUsersTask : MultipleIterationTask` overriding `RunMultipleIterationTaskAsync(int currentIteration)`; constants `public const int MaxTitleLength = 200;` and `public const int MaxMessageLength = 2000;`.

- [ ] **Step 1: Write the failing tests** in `TestNotifyUsersTask` (`[DoNotParallelize]`; `[TestInitialize]` installs a fresh `RecordingUserNotifier` via `Core.Init(PluginInstanceLogger.LogPath, userNotifier: _notifier)` after `Common.ConfigureLogPath()`; `[TestCleanup]` calls `Core.Init(PluginInstanceLogger.LogPath)`). Run the task as other plugin tests do: `new NotifyUsersTask { Config = config, ParentFolder = Common.CreateRootFolder() }`, `Common.GetTaskDefaultParameters(task)`, `await task.RunAsync(chain, set, null, logger, CancellationToken.None)`.
  - `Sends_title_message_and_level` — Title `"Backup"`, Message `"Done"`, Level `Warning`, Name `"Notify 1"` → one notification with those values, `SourceName == "Notify 1"`; the single `ExecResult.Result` is `true`.
  - `Resolves_dynamic_data_in_title_and_message` — set env var `OSR_NOTIFY_TEST` = `"srv01"` (unique name, removed in `finally`); Title `"Host {environment['OSR_NOTIFY_TEST']}"` → `"Host srv01"`; same for Message.
  - `Sends_one_notification_per_iteration` — `IterationsCount = 3` → 3 notifications, 3 successful results.
  - `Blank_title_fails_the_iteration` — Title `"   "`, Message `"x"`, 2 iterations → 2 results, both `Result == false`, no notification recorded. (That one failed iteration doesn't stop the next is the base class's contract, already covered by `TestTaskErrorHandling`.)
  - `Long_title_and_message_are_truncated` — Title of 250 `'t'`, Message of 2,500 `'m'` → lengths exactly 200 and 2,000.
  - `Notifier_exception_fails_the_iteration` — `FailWith = _ => new InvalidOperationException("hub down")` → `ExecResult.Result == false`.
  - `Cancellation_propagates_and_is_not_a_failure` — `CancellationTokenSource` cancelled inside `FailWith` (return `new OperationCanceledException(cts.Token)`), run with `cts.Token` → `Assert.ThrowsAsync<OperationCanceledException>`.
- [ ] **Step 2: Run** `dotnet test OSRobot.Tests --filter "FullyQualifiedName~TestNotifyUsersTask"` — expected: build FAILS (types don't exist).
- [ ] **Step 3: Implement** the plugin, config and task. In the task: read `(NotifyUsersTaskConfig)_iterationTaskConfig`; `string.IsNullOrWhiteSpace(Title)` → throw `ApplicationException` with a message naming the task; truncate with a small private static helper; `await Core.UserNotifier.NotifyAsync(new UserNotification(title, message, config.Level, DateTime.Now, config.Name), _cancellationToken)`; when `config.Log`, log one info line that the notification was sent to all connected editors (via `_instanceLogger`). Add the resource string to both `.resx` and `Resource.Designer.cs`. Update `TestPluginRegistry` to 25.
- [ ] **Step 4: Run** the filter — expected: all pass. Run `--filter "FullyQualifiedName~TestPluginRegistry|FullyQualifiedName~TestConfigCloning"` — expected: all pass (TestConfigCloning now includes `NotifyUsersTask`).
- [ ] **Step 5: Hand off for review** (do not commit).

---

### Task 3: SignalR hub and server wiring

**Files:**
- Create: `OSRobot.Server/Infrastructure/Notifications/NotificationsHub.cs`
- Create: `OSRobot.Server/Infrastructure/Notifications/HubUserNotifier.cs`
- Create: `OSRobot.Server/Infrastructure/Notifications/NotificationMessage.cs` (DTO)
- Modify: `OSRobot.Server/Program.cs` (JWT `Events`, `AddSignalR`, singleton `IUserNotifier`, `MapHub`)
- Modify: `OSRobot.Tests/OSRobot.Tests.csproj` (add `<ProjectReference Include="..\OSRobot.Server\OSRobot.Server.csproj" />`)
- Create: `OSRobot.Tests/Fakes/FakeSignalR.cs` (fakes for `IHubContext<NotificationsHub>`, `IHubClients`, `IClientProxy`, `HubCallerContext`)
- Test: `OSRobot.Tests/TestClasses/TestNotificationsHub.cs`

**Interfaces:**
- Consumes: `IUserNotifier`, `UserNotification` (Task 1); `OSRobotClaimTypes.MustChangePassword` (`"mustChangePassword"`, `OSRobot.Server/Infrastructure/Security/OSRobotClaimTypes.cs`).
- Produces (namespace `OSRobot.Server.Infrastructure.Notifications`):
  - `public const string NotificationsHub.Path = "/hubs/notifications";` and `public const string NotificationsHub.ClientMethod = "notification";`
  - `[Authorize] public class NotificationsHub : Hub` — overrides `OnConnectedAsync()` only; no other public methods.
  - `public sealed record NotificationMessage(string Title, string Message, string Level, DateTime Timestamp, string SourceName);` with `static NotificationMessage From(UserNotification n)` (`Level = n.Level.ToString()`).
  - `public sealed class HubUserNotifier(IHubContext<NotificationsHub> hubContext) : IUserNotifier` — `Clients.All.SendAsync(ClientMethod, NotificationMessage.From(n), cancellationToken)`.

- [ ] **Step 1: Write the failing tests** in `TestNotificationsHub`, using the fakes (fake `IClientProxy` records `(method, args)`; fake `HubCallerContext` has a settable `User` and records `Abort()`):
  - `Notifier_broadcasts_the_notification_to_all_clients` — one call, method `"notification"`, single arg equal to `new NotificationMessage("T", "M", "Error", ts, "Src")`.
  - `OnConnected_aborts_when_password_change_is_required` — user with claim `mustChangePassword = "true"` → `Aborted == true`.
  - `OnConnected_accepts_a_normal_user` — claim absent → `Aborted == false`.
- [ ] **Step 2: Run** `dotnet test OSRobot.Tests --filter "FullyQualifiedName~TestNotificationsHub"` — expected: build FAILS.
- [ ] **Step 3: Implement** the hub, notifier and DTO, then `Program.cs`:
  - In `AddJwtBearer(o => ...)` add `o.Events = new JwtBearerEvents { OnMessageReceived = ... }` setting `context.Token` from `Request.Query["access_token"]` only when `context.HttpContext.Request.Path.StartsWithSegments(NotificationsHub.Path)`; comment why (browsers cannot set headers on WebSockets; limited to the hub so API calls never accept tokens in URLs).
  - `builder.Services.AddSignalR();` and `builder.Services.AddSingleton<IUserNotifier, HubUserNotifier>();` next to the engine registration.
  - `app.MapHub<NotificationsHub>(NotificationsHub.Path);` after `app.MapControllers();`.
  - Hub comment: `MustChangePasswordFilter` is an MVC filter and does not run for hubs, hence the check in `OnConnectedAsync`.
- [ ] **Step 4: Run** the filter — expected: 3 passed. `dotnet build OSRobot.sln` — expected: no warnings. Full suite — expected: no new failures.
- [ ] **Step 5: Hand off for review** (do not commit).

---

### Task 4: Editor connection, presentation and permission menu

**Files:**
- Modify: `OSRobot.JobEditor/package.json` (`npm install @microsoft/signalr@8.0.7`)
- Modify: `OSRobot.JobEditor/src/infrastructure/server/ServiceBase.js` (new `ensureFreshToken()`)
- Create: `OSRobot.JobEditor/src/infrastructure/server/NotificationHub.js`
- Create: `OSRobot.JobEditor/src/infrastructure/NotificationPresenter.js`
- Create: `OSRobot.JobEditor/src/components/DesktopNotificationsMenuItem.vue`
- Modify: `OSRobot.JobEditor/src/layouts/MainLayout.vue` (start/stop hub; menu item above the separator before Logout)
- Modify: `OSRobot.JobEditor/src/i18n/en-US/index.js`

**Interfaces:**
- Consumes: server hub at `NotificationsHub.Path`, client method `"notification"`, payload `{ title, message, level, timestamp, sourceName }` (Task 3).
- Produces:
  - `ServiceBase.prototype.ensureFreshToken(): Promise<string>` — renews via the existing `_renewToken()` when `Utility.jwtShouldRenew(this._token)`, returns `this._token`; throws like `_renewToken` when renewal fails.
  - `NotificationHub.js` default export with `start(onNotification: (payload) => void): Promise<void>` and `stop(): Promise<void>`. URL: `AppConfig.apiUrl` with a trailing `/api` replaced by `/hubs/notifications`. Builder: `withUrl(url, { accessTokenFactory, withCredentials: false })`, `withAutomaticReconnect()`. `accessTokenFactory` builds a `ServiceBase` from `useAppStore().getLoggedUser()` and returns `ensureFreshToken()`. A failed initial `start` is caught and logged with `console.warn` (no redirect).
  - `NotificationPresenter.js` exports `present(payload)`, `getDesktopState(): "unsupported" | "default" | "granted-on" | "granted-off" | "denied"`, `requestOrToggleDesktop(): Promise<void>`. Storage key `OSRDesktopNotificationsMuted`. Unsupported when `!("Notification" in window) || !window.isSecureContext`. Visible tab → Quasar `Notify.create({ type, caption, message, html: false, timeout })` with `type` from level (`Info→info`, `Success→positive`, `Warning→warning`, `Error→negative`) and `timeout` `0` for `Error` (with a close action) else `8000`. Hidden + `granted-on` → `new Notification(title, { body: message })`, `onclick` → `window.focus()`. Hidden otherwise → push to an in-memory list flushed on `document` `visibilitychange` to `visible`.
  - `DesktopNotificationsMenuItem.vue` — a `q-item` showing the state label; click → `requestOrToggleDesktop()`; disabled with the "requires HTTPS or localhost" caption when unsupported; caption with re-enable hint when denied.
  - i18n keys: `desktopNotifications`, `desktopNotificationsOn`, `desktopNotificationsOff`, `desktopNotificationsBlocked`, `desktopNotificationsBlockedHint`, `desktopNotificationsUnsupported`.

- [ ] **Step 1: Implement** the files above (no editor unit-test framework exists: `npm test` is a stub).
- [ ] **Step 2: Run** `npm run lint` in `OSRobot.JobEditor/` — expected: no errors. Run `quasar build` — expected: succeeds.
- [ ] **Step 3: Smoke check** with the server running (`dotnet run --project OSRobot.Server`) and `quasar dev`: after login the browser devtools Network tab shows a `101 Switching Protocols` WebSocket to `/hubs/notifications?...access_token=...`; logging out closes it.
- [ ] **Step 4: Hand off for review** (do not commit).

---

### Task 5: Editor task form

**Files:**
- Create: `OSRobot.JobEditor/src/robotObjects/notifyUsersTask/NotifyUsersTaskConfigForm.vue`
- Modify: `OSRobot.JobEditor/src/layouts/MainLayout.vue` (import + component map entry next to `PingTaskConfigForm`)
- Modify: `OSRobot.JobEditor/src/infrastructure/workspace/RobotObjectUtility.js` (`case "NotifyUsersTask": return "notifications";`)
- Modify: `OSRobot.JobEditor/src/i18n/en-US/index.js` (keys `title`, `message`, `level`, `levelInfo`, `levelSuccess`, `levelWarning`, `levelError` — reuse any that already exist)

**Interfaces:**
- Consumes: config JSON fields `title`, `message`, `level` (string enum values `Info`/`Success`/`Warning`/`Error`) from Task 2.
- Produces: `NotifyUsersTaskConfigForm` component looked up as `` `${pluginId}ConfigForm` ``.

- [ ] **Step 1: Implement** the form following `pingTask/PingTaskConfigForm.vue` (`PluginGeneralConfigForm`, `q-card` "configuration"): Title `q-input` (mandatory rule) + `BtnDynamicDataBrowser` (`modelValueKey="title"`), Message `q-input type="textarea"` + `BtnDynamicDataBrowser` (`modelValueKey="message"`), Level `q-select` (emit-value, map-options) with the four levels; then `PluginIterationConfigForm` as in `zipTask/zipTaskConfigForm.vue`.
- [ ] **Step 2: Run** `npm run lint` — expected: no errors.
- [ ] **Step 3: Check** in `quasar dev`: "Notify users" appears in the palette with the bell icon; add it, set fields, save; reopen → values kept; `Data/jobs.json` contains `"pluginId": "NotifyUsersTask"` with `title`, `message`, `level`.
- [ ] **Step 4: Hand off for review** (do not commit).

---

### Task 6: Documentation and end-to-end verification

**Files:**
- Modify: `docs/user-manual/10-tasks.md` (new "Notify users" section, same structure as neighbouring task sections)
- Modify: `SECURITY.md` and `docs/user-manual/12-security.md` (token-in-URL note)
- Modify: `CLAUDE.md` (one line under "Core runtime model" on `Core.UserNotifier` / `IUserNotifier`; fix the config-form path note: existing forms are `src/robotObjects/<camelCaseId>/<Id>ConfigForm.vue`, e.g. `pingTask/PingTaskConfigForm.vue`)

- [ ] **Step 1: Write docs.** User manual: fields (Title, Message, Level), one notification per iteration, requirements (an editor open — background/minimized is fine; HTTPS or `http://localhost`; browser permission via the user menu), notifications are lost when no editor is open, mute is per browser. Security: the JWT travels as `access_token` in the WebSocket URL to `/hubs/notifications`; OS-Robot's logs exclude it at the default `Microsoft.AspNetCore: Warning`; reverse proxies usually log query strings — exclude `access_token` from IIS/nginx logs; exposure limited by short token lifetime.
- [ ] **Step 2: End-to-end run** (`dotnet run --project OSRobot.Server` + `quasar dev`), job: DateTimeEvent (object 1) every minute → Notify users (Title `"OS-Robot test"`, Message `"Year {object[1].ExecutionStartDateYear}"`, Level Error). Check and note the result of each:
  - tab visible → in-app error notification that stays until closed;
  - tab hidden, permission granted → system notification; clicking focuses the tab;
  - menu item states: default → click asks permission; granted toggles On/Off; Off + hidden tab → no system notification, in-app ones appear on return;
  - permission blocked in browser settings → "Blocked" with hint;
  - login as a user with must-change-password → no WebSocket connection accepted (hub aborts);
  - stop and restart the server with the tab open → reconnects and next notification arrives;
  - open the editor via the machine's LAN IP over HTTP → item disabled "requires HTTPS or localhost", in-app notifications still shown.
- [ ] **Step 3: Run** `dotnet build OSRobot.sln` (no warnings), `dotnet test OSRobot.Tests/OSRobot.Tests.csproj` (no new failures), `npm run lint` (clean).
- [ ] **Step 4: Hand off for review** (do not commit) with the end-to-end results.
