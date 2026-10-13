# 9. Events reference

All events share the General section (Name, Enabled, Log) and the
[standard dynamic data fields](08-dynamic-data.md#84-standard-fields-of-every-object). The tables
below list the specific settings.

## 9.1 Date/Time event

Fires at a given date and time and, optionally, repeatedly afterwards. **Platforms:** all.

| Setting | Description |
|---|---|
| At date | First date and time the event fires. If it is in the past when the jobs are loaded, recurring timing starts immediately. |
| One time | Fire once, at the given date. |
| Every (days / hours / minutes) | Repeat at this interval after the first occurrence. |
| Every (seconds) | Repeat every N seconds. |
| On days | Fire only on the selected weekdays, or on **All days**. |

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
