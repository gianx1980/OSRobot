# 3. Server configuration

Server settings are read from `OSRobot.Server/appsettings.json`. Rather than editing that file, put
your overrides in `appsettings.Production.json` next to it: only the keys you write there replace
the defaults. Restart the server after changing configuration files.

## 3.1 Listening address (Kestrel)

By default the server listens on `http://localhost:7098`:

```json
"Kestrel": {
  "Endpoints": {
    "MyHttpEndpoint": { "Url": "http://localhost:7098" }
  }
}
```

To make the editor reachable from other machines, put HTTPS in front of the server (IIS,
nginx/Caddy or Kestrel with a certificate). See [Security and deployment](12-security.md) and
[DEPLOYMENT.md](../../OSRobot.Server/DEPLOYMENT.md).

## 3.2 Job engine (`AppSettings:JobEngineConfig`)

| Setting | Default | Description |
|---|---|---|
| `LogPath` | `.\ExecLogs` | Folder for execution logs. |
| `DataPath` | `.\Data` | Folder that contains `jobs.json`. |
| `SerialExecution` | `false` | When `true`, tasks are executed one at a time (a single worker), regardless of `MaxConcurrentTasks`. |
| `MaxConcurrentTasks` | `16` | Maximum number of tasks that can run at the same time. |
| `CleanUpLogsOlderThanHours` | `24` | Execution logs older than this are deleted automatically. |
| `CleanUpLogsIntervalHours` | `1` | How often the log clean-up runs. |
| `StopDrainTimeoutSeconds` | `30` | When the server stops, how long running tasks are given to finish before being cancelled. |
| `ScriptingEnabled` | `true` | Set to `false` to disable `[CODE]` C# expressions in dynamic data (see [Dynamic data](08-dynamic-data.md#86-c-expressions-code)). |
| `RunProgramAllowedExecutablePaths` | empty | Comma-separated list of executables or folders the Run program task may start. Empty means no restriction. |

## 3.3 Authentication and session

| Setting | Default | Description |
|---|---|---|
| `JWT:Key` | sample key | Secret used to sign login tokens. **Replace it** with a long random string. |
| `JWT:Issuer` / `JWT:Audience` | os-robot.com values | Token issuer and audience. Normally left unchanged. |
| `JWT:ExpireInMinutes` | `5` | Lifetime of an access token. |
| `JWT:RequestNewTokenIfMinutesLeft` | `1` | The editor renews the token when fewer minutes than this are left. |
| `RefreshToken:ExpireInMinutes` | `1440` | Lifetime of the refresh token (how long a session can last without logging in again). |
| `Security:MaxFailedLoginAttempts` | `5` | Failed logins before the account is locked. |
| `Security:LockoutDurationMinutes` | `15` | How long the lockout lasts. |

## 3.4 Editor and proxy settings

| Setting | Default | Description |
|---|---|---|
| `ClientSettings:AppTitle` | `OSRobot` | Title shown by the editor. |
| `ClientSettings:StaticFilesUrl` | URL | Address of the server's static files, used by the editor. |
| `ClientSettings:HeartBeatInterval` | `2500` | Milliseconds between the editor's connection checks. If the server stops answering, the editor shows *"Connection to server lost - Trying to reconnect..."*. |
| `ClientSettings:NotificationServerSentEventsEnabled` | `true` | Use server-sent events for notifications. |
| `ClientSettings:NotificationPollingInterval` | `30000` | Polling interval (ms) used when server-sent events are not available. |
| `ReverseProxy:KnownProxies` | empty | Comma-separated IPs of reverse proxies running on another machine. |
| `ReverseProxy:KnownNetworks` | empty | Comma-separated CIDR networks of trusted proxies, e.g. `10.0.0.0/24`. |

---

Previous: [2. Installation and first start](02-installation.md) · Next: [4. The Job Editor at a glance](04-job-editor.md)
