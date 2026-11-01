# 9. Events reference

All events share the General section (Name, Enabled, Log) and the
[standard dynamic data fields](08-dynamic-data.md#84-standard-fields-of-every-object). The tables
below list the specific settings.

## 9.1 Date/Time event

Fires on a schedule. **Platforms:** all.

Choose one of three schedules:

| Setting | Description |
|---|---|
| One time | Fire once, at **At date**. |
| Every day at | Fire every day at the given **Time** (`HH:mm:ss`), on the selected weekdays. |
| Every (hours / minutes / seconds) | Fire repeatedly at this interval, on the selected weekdays. Occurrences are aligned to **Starting from**: every 15 minutes starting from 10:00 fires at 10:00, 10:15, 10:30... even after a restart or a reload. If *Starting from* is in the future, nothing fires until then. |
| At date / Starting from | The date and time of the one-time occurrence, or the first occurrence of the interval. |
| On days | The weekdays an **Every day at** or **Every** schedule fires on, or **All days**. Not used by **One time**. |

Timing details:

- **Precision.** Occurrences fire within a few milliseconds of the scheduled time, and don't drift
  over time.
- **Missed occurrences are skipped.** If OS-Robot was not running, the computer was asleep or the
  clock was moved forward at the scheduled time, that occurrence does not fire later: the event
  waits for the next one. The same applies when the jobs are loaded: a *One time* event whose date
  has passed never fires.
- **Daylight saving time.** *Every day at* keeps the same clock time across the change (02:00
  stays 02:00). A time that doesn't exist on the day the clocks move forward fires when the clock
  jumps; a time that happens twice when the clocks move back fires once. An interval stays evenly
  spaced in real time.

**Dynamic data:** standard fields only (the start date is the time the event fired).

## 9.2 CPU event

Monitors the total processor usage of the machine, all processors together. **Platforms:** Windows,
Linux.

| Setting | Description |
|---|---|
| Trigger if usage is above threshold (%) | Fires when a sample exceeds the threshold. |
| Trigger if average usage is above threshold (%) in the last N minutes | Fires when the average over the period exceeds the threshold. |
| Trigger if the following number of minutes passed since the last trigger | Limits how often the event can fire, to avoid floods of notifications. |
| Check every | Sampling interval, in seconds. |

Usage is measured between two checks, so the first check after the server starts (or the jobs are
reloaded) only takes the starting point: the first value comes one interval later.

- **Windows** reads the same *Processor utility* value Task Manager shows.
- **Linux** reads `/proc/stat`, as `top` does. Inside a container this is the host's processor usage,
  not the container's share.
- **macOS** is not supported yet.

**Dynamic data:** `CpuUsagePercentage`.

## 9.3 Memory event

Same settings as the CPU event, applied to memory usage. **Platforms:** all.

**Dynamic data:** `MemoryUsagePercentage`.

## 9.4 Disk space event

Checks the space of one or more disks at a regular interval. **Platforms:** all.

| Setting | Description |
|---|---|
| Check the following disks | List of disk checks. Each check has a **Disk**, an operator (**Greater than** / **Less than**), a value and a unit (**Megabytes**, **Gigabytes**, **Terabytes** or **Percentage**). |
| Check every | Interval between checks, in seconds. |

**Dynamic data:** `DiskName`, `DiskSpaceBytes`.

## 9.5 File system event

Watches folders and fires when files are created, modified or deleted. **Platforms:** all.

| Setting | Description |
|---|---|
| Monitor the following folders | List of folders. For each one: **Path**, **Monitor subfolders**, and what to watch: **New files**, **Modified files** or **Deleted files**. |

**Dynamic data:** `FullPathName`, `FileName`, `FileNameWithoutExtension`, `FileExtension`,
`ChangeType`.

## 9.6 System events event

Fires on operating-system notifications of the interactive session. Select one or more of:
**Display settings changed**, **Installed fonts changed**, **Palette changed**, **Power mode
changed**, **Session ended**, **Session switch**, **Time changed**, **User preference changed**.
These notifications are produced by Windows; on other systems they may never fire.

**Dynamic data:** `EventCode` - the name of the notification, e.g. `TimeChanged`.

## 9.7 OSRobot service start event

Fires when the OS-Robot server starts. Useful to run start-up jobs or to be warned of unexpected
restarts. **Platforms:** all.

| Setting | Description |
|---|---|
| Trigger if service starts within the following number of minutes from computer boot | Fires only when the server starts shortly after the computer booted (e.g. after a reboot). |
| Trigger if service starts after the following number of minutes from computer boot | Fires only when the server starts long after boot (e.g. a manual restart or a crash recovery). |

## 9.8 Task failure event

Fires when tasks fail, so a single job can report the failures of many - with an e-mail, a
notification, a call to a chat - instead of an *Object does not execute* connection on every task.
**Platforms:** all.

Place it in a folder and connect it to the tasks that should react, as with any event.

| Setting | Description |
|---|---|
| Watch | Whose failures to report: **This folder and its subfolders** (the default), **This folder only** or **All folders**, relative to the folder the event is in. |
| Group failures for (seconds) | **0** (the default): the event fires once per failure. Otherwise the first failure opens a window of this many seconds, and at its end the event fires **once** with every failure of the window. The window closes after that time whatever happens, so a task failing over and over is still reported every window. |

Each failed iteration counts as one failure. Every failure in scope is reported, including those the
job already reacts to with an *Object does not execute* connection.

**Dynamic data:**

| Field | Content |
|---|---|
| `FailedObjectID`, `FailedObjectName`, `FailedObjectType` | The task that failed (the first one of the window when grouping). |
| `FailedObjectFolder` | Its folder, e.g. `Backups/Nightly`; empty for the root folder. |
| `FailureMessage` | Why it failed. Secrets are hidden as `***`. |
| `FailedIterationIndex` | The iteration that failed; `-1` when the task failed as a whole. |
| `FailureDate` | When it failed. |
| `FailureCount` | How many failures this occurrence reports. |
| `Failures` | Recordset with one row per failure: `ObjectID`, `ObjectName`, `ObjectType`, `Folder`, `Message`, `IterationIndex`, `FailureDate`. At most 500 rows; `FailureCount` still counts them all. |

Things to know:

- **Leaving tasks out.** A job that already reports its own failures would be reported twice. Either
  keep it outside the event's scope (another folder, or *Watch*: *This folder only*), or add a
  *don't execute* condition to the event's connection, for example `FailedObjectName` *Value equals
  to* `Nightly backup`, or `FailedObjectFolder` *Value starts with* `Backups`.
- **The event's own job is not watched.** If the tasks connected to a Task failure event fail (for
  example the mail server is down), that failure is logged but doesn't fire the event again, which
  would otherwise loop forever.
- **When the server stops or reloads the jobs while a window is open**, the failures collected so
  far are not reported: the event's log says how many.
- `Failures` is not the default recordset: a Send e-mail or Notification task connected to the
  event sends one message, not one per row. To list the failures in the message, use a C#
  expression over `dynamicDataChain[ID]["Failures"]`, or iterate over the recordset on purpose.

## 9.9 Webhook event

Fires when another system calls `POST /hooks/{path}` on this server: a build server when a build
ends, an application when an order is confirmed, a script on another machine. The body of the call
reaches the job as dynamic data. **Platforms:** all.

| Setting | Description |
|---|---|
| Path | The last part of the URL, for example `deploy-done`. Letters, digits, `_` and `-`. A random one is proposed. Each enabled hook needs its own. |
| Authentication | **Token in a header**: the caller sends `Authorization: Bearer <token>` or `X-OSRobot-Token: <token>`. **Signature of the body (HMAC-SHA256)**: the caller signs the body with the key, as GitHub does; the signature, in hexadecimal or base64, with or without `sha256=` in front, travels in the header you choose (default `X-Hub-Signature-256`). |
| Token / Signing key | **Generate token** creates a random one and stores it as a [secret](08-dynamic-data.md#87-variables-and-secrets). It is shown only then: copy it for the caller. Without a token every call is refused. |
| Maximum calls per minute | Calls accepted for this hook; default 60, 0 for no limit. |
| JSON Path to data / Returns recordset | As in the [REST API task](10-tasks.md#rest-api): extract a value or a list of rows from a JSON body. With a JSON path set, a body that is not JSON is refused. |

**Calling it:**

```bash
curl -X POST -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{\"order\": 123}" http://localhost:7098/hooks/deploy-done
```

The answer comes right away, without waiting for the job:

| Answer | Meaning |
|---|---|
| `202 Accepted` | The job was started. |
| `404 Not Found` | No such hook, the hook or its folder is disabled, **or the token or signature is wrong**: nothing reveals which hooks exist. Wrong tokens are written to the audit log. |
| `400 Bad Request` | The body is not JSON although a JSON path is set. |
| `413 Payload Too Large` | The body exceeds the limit (1 MB by default). |
| `429 Too Many Requests` | Too many calls, for this hook or from this address. |
| `503 Service Unavailable` | The server is reloading or stopping: retry shortly. |

**Dynamic data:**

| Field | Content |
|---|---|
| `RequestBody` | The body of the call, as text. |
| `ContentType`, `SourceIp` | Its content type, and the caller's address. |
| `RequestHeaders`, `QueryParameters` | Recordsets with columns `Name` and `Value`. Credentials are left out: `Authorization`, the token and signature headers, cookies. |
| `JsonPathData`, `DefaultRecordset` | What the JSON path extracted. |

> [!WARNING]
> **The body comes from outside: treat it as untrusted.** Placeholders inside it are never expanded,
> so it can't read a secret. But pasting `{object[N].RequestBody}` into a SQL query, or into the
> arguments of a Run program task, lets the caller inject commands. In SQL, pass it as a parameter.

Things to know:

- **Calls from this machine work right away.** By default the server listens on `localhost` only. To
  let other machines call a hook, without exposing the editor or the API, see
  [DEPLOYMENT.md](../../OSRobot.Server/DEPLOYMENT.md#letting-other-machines-call-the-webhooks).
- **Signatures:** the key signs the whole body, GitHub's scheme. Services that sign something else,
  such as Stripe (a timestamp plus the body), are not supported yet. A signature doesn't expire: a
  recorded call can be sent again.
- Files (multipart bodies) are not supported, and calls can't wait for the job's result.

---

Previous: [8. Dynamic data](08-dynamic-data.md) · Next: [10. Tasks reference](10-tasks.md)
