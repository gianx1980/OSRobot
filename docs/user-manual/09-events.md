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

Monitors processor usage. **Platforms:** Windows only.

| Setting | Description |
|---|---|
| Trigger if usage is above threshold (%) | Fires when a sample exceeds the threshold. |
| Trigger if average usage is above threshold (%) in the last N minutes | Fires when the average over the period exceeds the threshold. |
| Trigger if the following number of minutes passed since the last trigger | Limits how often the event can fire, to avoid floods of notifications. |
| Check every | Sampling interval, in seconds. |

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

---

Previous: [8. Dynamic data](08-dynamic-data.md) · Next: [10. Tasks reference](10-tasks.md)
