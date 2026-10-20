# Browser notifications ("Notify users" task) — design

Date: 2026-10-20 · Status: approved in conversation, pending written-spec review

## Goal

Let a job inform people without email: a new **Notify users** task sends a short, one-way
message that appears in every open OS-Robot editor — as a system notification when the editor
tab is in the background, as an in-app notification when it is in front.

### Decisions already taken

| Question | Decision |
|---|---|
| Purpose | Inform only (one-way). No answers, no job waiting on the user. |
| Channel | The browser, via the editor. Not email, not a per-user desktop agent, not `WTSSendMessage`. |
| Variant | Editor tab must be open (background/minimized is fine). No Web Push, no service worker, no third-party push service. |
| Recipients | Every signed-in user with an editor open. No per-task recipient list (can be added later, optional, without breaking jobs). |
| Missed notifications | Not stored. If no editor is open, the notification is lost. |

### Why not the alternatives

- **Desktop of the Windows user at the PC:** OS-Robot runs as a Windows service (Session 0), which
  cannot show UI on a user's desktop. Reaching it needs `LocalSystem` (`CreateProcessAsUser`) or
  the session "Message" permission (`WTSSendMessage`), both against the installation guide's
  advice to use a dedicated low-privilege account; or a separate per-user agent to build and install.
- **Web Push:** works with the tab closed, but routes every notification through the browser
  vendor's push servers and needs internet access, VAPID keys and PWA mode.

## Architecture

```
Job runs NotifyUsersTask
  → Core.UserNotifier.NotifyAsync(notification, ct)        OSRobot.Server.Core   (interface, no-op default)
  → HubUserNotifier → IHubContext<NotificationsHub>        OSRobot.Server        (ASP.NET / SignalR)
      .Clients.All.SendAsync("notification", dto)
  → NotificationsHub  /hubs/notifications  [Authorize]     JWT, refuses MustChangePassword tokens
  → editor NotificationHub.js (@microsoft/signalr)
      tab visible → Quasar notification; tab hidden + permission → system Notification
```

Core and plugins stay free of ASP.NET; tasks have no DI, so the notifier is reached through the
static `Core`, like `Core.ScriptingEnabled`.

### OSRobot.Server.Core

- `IUserNotifier` with `Task NotifyAsync(UserNotification notification, CancellationToken cancellationToken)`.
- `UserNotification` record: `Title`, `Message`, `Level`, `Timestamp`, `SourceName` (the sending
  task's `Config.Name`).
- `UserNotificationLevel` enum: `Info` (default), `Success`, `Warning`, `Error`.
- `NullUserNotifier`: completes immediately; the default.
- `Core.UserNotifier { get; private set; }`, defaulting to `NullUserNotifier`, set through a new
  optional `userNotifier` parameter of `Core.Init` (null → `NullUserNotifier`). Existing callers
  are unaffected.

### OSRobot.Server.JobEngineLib

- `JobEngine` gains an optional constructor parameter `IUserNotifier? userNotifier = null` and passes
  it to `Core.Init` in `Start()`. The DI container supplies the server's implementation; tests and
  `EngineHarness` keep working unchanged (no-op) or pass a fake.

### OSRobot.Server

- `NotificationsHub : Hub`, `[Authorize]`, mapped at `/hubs/notifications`. Server-to-client only:
  it declares no client-callable methods. `OnConnectedAsync` aborts the connection when the user's
  `MustChangePassword` claim is `"true"` (`MustChangePasswordFilter` is an MVC filter and does not
  cover hubs).
- `HubUserNotifier : IUserNotifier` sends `"notification"` with a DTO
  `{ title, message, level, timestamp, sourceName }` (level as a string) to `Clients.All`.
  Registered as the singleton `IUserNotifier`.
- `Program.cs`: `AddSignalR()`, `MapHub<NotificationsHub>("/hubs/notifications")`, and a JWT
  `Events.OnMessageReceived` that reads the `access_token` query parameter **only** when the request
  path starts with `/hubs/notifications`.

## The "Notify users" task (OSRobot.Server.Plugins/NotifyUsersTask/)

- `NotifyUsersTaskPlugin`: `Id = "NotifyUsersTask"`, title from `Resource` ("Notify users"),
  `PluginType` task, `SupportedOSPlatforms = All`, `SampleDynamicData` = the standard samples only.
- `NotifyUsersTaskConfig : ITaskConfig`: standard members (`Id`, `Name`, `Enabled`, `Log`,
  `PluginIterationMode`, `IterationObject`, `IterationsCount`) plus
  - `[DynamicData] string Title` (required)
  - `[DynamicData] string Message` (optional)
  - `[JsonConverter(typeof(JsonStringEnumConverter))] UserNotificationLevel Level = Info`
  
  All with public getters and setters (see CLAUDE.md: get-only members are dropped on load).
- `NotifyUsersTask : MultipleIterationTask`: one notification per iteration, from
  `_iterationTaskConfig` (dynamic data already resolved by the base class).
  - Empty or whitespace title after resolution → throw → that iteration fails.
  - Title truncated to 200 characters, message to 2,000.
  - `await Core.UserNotifier.NotifyAsync(..., _cancellationToken)`.
  - No editor connected is **success**. A send exception propagates → the iteration fails (task
    error contract), so jobs can react with a "failed" connection. Cancellation propagates.
  - Output: only the standard dynamic data. No "delivered" field — the server cannot know whether
    anyone saw it.

## Editor (OSRobot.JobEditor)

- Dependency: `@microsoft/signalr`.
- `src/infrastructure/server/NotificationHub.js`:
  - Hub URL derived from `AppConfig.apiUrl` (trailing `/api` replaced by `/hubs/notifications`).
  - `accessTokenFactory`: renews the stored token when `Utility.jwtShouldRenew` says so (same
    flow as `ServiceBase._renewToken`), then returns the current token.
  - `withAutomaticReconnect()`; `withCredentials: false` (token travels in the URL; compatible with
    both CORS policies).
  - Started in `MainLayout` `onMounted`, stopped in `onUnmounted` (logout leaves the layout). If
    the token cannot be renewed it stops quietly; normal API calls already handle sending the user
    back to login.
- Display, on `"notification"`:
  - Tab visible → Quasar `Notify`, type by level (`info`/`positive`/`warning`/`negative`), title +
    message as plain text (`html: false`). `Error` stays until dismissed; others close after 8 seconds.
  - Tab hidden, permission granted and not muted → `new Notification(title, { body: message })`;
    click → `window.focus()`.
  - Tab hidden otherwise → kept in an in-memory list and shown as Quasar notifications on the next
    `visibilitychange` to visible (so they don't time out unseen).
  - Mute only silences system notifications; in-app notifications are always shown.
- Permission control: a user-menu item (next to Logout) **Desktop notifications: On / Off /
  Blocked**.
  - First click calls `Notification.requestPermission()` (needs a user gesture).
  - Once granted, it toggles a per-browser mute flag in `localStorage`.
  - Blocked → explains how to re-enable in the browser.
  - No `Notification` API or `window.isSecureContext === false` → item disabled, "requires HTTPS or
    localhost".
- Task form: `src/robotObjects/notifyUsersTask/NotifyUsersTaskConfigForm.vue` (Title, Message,
  Level + common settings), entry in the `MainLayout.vue` component map, `notifications` icon in
  `RobotObjectUtility.js`, strings in `src/i18n/en-US/index.js`.

## Security

- Only authenticated users who have changed the seeded password receive notifications — no wider
  than today, since they can already read every job's configuration.
- The JWT is in the WebSocket URL (`?access_token=`). OS-Robot's own logs exclude it
  (`Microsoft.AspNetCore` at `Warning`), but reverse proxies (IIS, nginx) usually log query strings.
  Exposure is limited by the short token lifetime. SECURITY.md documents it and the mitigation
  (exclude `access_token` from proxy logs).
- Message text is rendered as plain text only. Clients cannot send anything to the hub.
- System notifications require a secure context: HTTPS, or `http://localhost` on the server itself.

## Testing

- `OSRobot.Tests/TestPlugins/TestNotifyUsersTask.cs` (`[DoNotParallelize]`, since `Core.UserNotifier`
  is process-wide), with a recording fake `IUserNotifier` installed through `Core.Init`:
  dynamic data resolved in title and message; level passed through; one notification per
  iteration; empty title fails the iteration; truncation; notifier exception fails the iteration;
  cancellation propagates and is not a failure.
- Engine: a job event → NotifyUsersTask reaches the fake passed to `JobEngine`.
- Server: new project reference `OSRobot.Tests` → `OSRobot.Server`; with hand-written fakes for the
  SignalR abstractions: `HubUserNotifier` broadcasts `"notification"` with the expected DTO; the hub
  aborts a connection whose user carries `MustChangePassword=true` and accepts a normal one.
- `TestConfigCloning` and `TestPluginRegistry` cover the new config/plugin automatically.
- Editor: `npm run lint`; manual end-to-end with a job that notifies every minute: tab visible, tab
  hidden, permission granted / denied / blocked, muted, must-change-password login, and a plain-HTTP
  remote origin (item disabled).

## Documentation

- `docs/user-manual/10-tasks.md`: "Notify users" section — fields, levels, requirements (editor
  open, HTTPS or localhost, browser permission), lost-when-nobody-is-connected behaviour.
- `SECURITY.md` and `docs/user-manual/12-security.md`: token-in-URL note and proxy-log mitigation.
- `CLAUDE.md`: `Core.UserNotifier` in the core runtime model.

## Out of scope

Per-task recipient lists, notification history or delivery when nobody is connected, Web Push,
answers from the user, and reaching the Windows desktop user directly.
