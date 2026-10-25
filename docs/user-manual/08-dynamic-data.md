# 8. Dynamic data

Dynamic data lets a task use values produced by the objects that ran before it in the same job
run. Any text setting that supports dynamic data accepts placeholders that are replaced just
before the task runs.

## 8.1 Syntax

| Placeholder | Meaning |
|---|---|
| `{object[ID].Field}` | A simple field of object ID, e.g. `{object[3].FileName}`. |
| `{object[ID].Recordset['Column']}` | Column of the current row of a recordset (the row of the current iteration). |
| `{object[ID].Recordset[N]['Column']}` | Column of row N (0-based). |
| `{object[ID].Recordset[{iterationIndex}]['Column']}` | Explicitly the row of the current iteration (same as omitting the row). |
| `{object[ID].Recordset[{subInstanceIndex}]['Column']}` | The row matching the iteration of the upstream task that started this run (useful with *Once for each result*). |
| `{environment['NAME']}` | Value of the server's environment variable NAME. |
| `{var['NAME']}` | Value of the variable NAME (see 8.7). |
| `{secret['NAME']}` | Value of the secret NAME, stored encrypted on the server (see 8.7). |
| `[CODE]...` | The whole setting is a C# expression (see 8.6). |

Examples:

```text
C:\Backup\{object[1].ExecutionStartDateUnderscoreDate}\report.csv
{object[5].DefaultRecordset['CustomerName']}
{object[5].DefaultRecordset[0]['Total']}
{environment['TEMP']}\work
{var['ShareRoot']}\exports\{object[1].ExecutionStartDateUnderscoreDate}.csv
{secret['SmtpPassword']}
```

Placeholders are replaced in a single pass: what a placeholder is replaced with is never read
again. A value that comes from outside - a line of a file, a REST response, an e-mail - and happens
to contain `{secret['X']}` or `{environment['X']}` stays as that text.

## 8.2 Which data is available

A task can use the data of every object that ran before it in the same chain: the event that
started the run and every task between that event and the current one. Data of objects in other
branches, or that run later, is not available - the editor's save validation reports such
references as warnings.

> [!TIP]
> Use the **Browse dynamic data** button (lightning bolt) next to a field to pick an object and a
> field from a list instead of typing the placeholder.

## 8.3 Recordsets and iterations

Tasks that read data (text files, Excel, SQL queries, REST APIs, directory listings...) return a
table called `DefaultRecordset`. A following task set to iterate over that recordset runs once per
row, and `{object[ID].DefaultRecordset['Column']}` returns the value of the current row each time.

## 8.4 Standard fields of every object

| Field | Content |
|---|---|
| `ObjectName` / `ObjectID` | Name and Id of the object. |
| `ExecutionResult` | Whether the execution succeeded. |
| `ExecutionReturnValue` | Numeric return value (0 = success). |
| `ExecutionStartDate`, `ExecutionEndDate` | Start and end date/time. |
| `...Year`, `...Month`, `...Day`, `...Hour`, `...Minute`, `...Second`, `...Ticks` | Parts of the start/end date, e.g. `ExecutionStartDateYear`. |
| `ExecutionStartDateUnderscore` | Start date as `yyyy_MM_dd_HH_mm_ss` - handy in file names. Also `...UnderscoreDate` (`yyyy_MM_dd`) and `...UnderscoreTime` (`HH_mm_ss`). The same fields exist for the end date. |
| `NumberOfIterations` | How many iterations the task ran. |
| `IterationResults` (tasks) | Recordset with one row per iteration; filled when a connection uses *Once, after all iterations*. |

## 8.5 Plugin-specific fields

Each plugin adds its own fields; they are listed in [Events](09-events.md), [Tasks](10-tasks.md)
and in the dynamic data browser.

## 8.6 C# expressions ([CODE])

If a setting starts with `[CODE]`, everything after it is evaluated as a C# script whose result (a
string) becomes the value. The script can use:

- `dynamicDataChain` - the data of the upstream objects, indexed by Object Id, e.g.
  `dynamicDataChain[3]["FileName"]`;
- `iterationNumber` - the current iteration (0-based);
- `subInstanceIndex` - the upstream iteration index, or null.

```csharp
[CODE]DateTime.Now.AddDays(-1).ToString("yyyyMMdd")

[CODE]dynamicDataChain[3]["FileName"].ToString().ToUpper()
```

The **Code editor** dialog offers a larger editing area for these expressions.

> [!WARNING]
> C# expressions have full access to the server (files, network, processes) with the rights of the
> service account. Disable them with `ScriptingEnabled: false` if your jobs do not need them.

## 8.7 Variables and secrets

Variables and secrets are named values shared by all jobs and kept on the server, outside the jobs.
Manage them with the **Variables and secrets** button in the toolbar.

- A **variable** is a plain value, visible to everyone who uses the editor: a server name, a
  shared folder, a base URL. Use it as `{var['Name']}`.
- A **secret** is stored encrypted, and its value is never shown again, not even to whoever
  entered it: passwords, API keys, tokens. Use it as `{secret['Name']}`. To change it, enter a new
  value; leave the field empty to keep the current one.

Names start with a letter or `_` and contain only letters, digits, `_`, `.` and `-`. Upper and
lower case are the same name. A variable and a secret may share a name. In the **Dynamic data
editor** (lightning bolt next to a field), **Insert variable or secret** inserts the placeholder
for you.

Things to know:

- **Changes apply immediately.** The next task that runs uses the new value: there is nothing to
  reload.
- **A name that doesn't exist makes the task fail**, with *"Variable 'X' is not defined"* in the
  log. Saving the job warns about such names, like other broken references.
- **Secrets are hidden in logs and notifications.** A secret's value (4 characters or more) found in
  an execution log, in the server log or in a Notification task is written as `***`. This is a
  safety net, not a guarantee: a secret that was transformed (encoded, split, cut) is not
  recognized.
- **Not available inside `[CODE]`.** C# expressions don't see variables or secrets.
- **Secrets are not access control.** Anyone who can edit jobs can use any secret, including
  sending it to a server of their own. Secrets keep values out of `jobs.json`, out of backups of it
  and off the screen. See [SECURITY.md](../../SECURITY.md).

### Moving passwords out of jobs.json

Saving a job warns about every password written in plain text. Next to the password of the SQL
Server, FTP/SFTP and Send e-mail tasks, the lock button **Move to a secret** stores the password
as a secret and replaces it with `{secret['Name']}`. Save the job afterwards.

> [!NOTE]
> `Data\jobs.json.bak` keeps the previous version of the jobs, password included, until the next
> save replaces it. Save a second time, or delete the file.

### Where secrets are kept

Secrets are stored in `Data\config.db`, encrypted with keys kept in the `Data\keys` folder. **Back
up the two together**: secrets can't be read without the keys, and would have to be entered
again (the dialog marks them *Cannot be decrypted*). How the keys themselves are protected is
explained in [Server configuration](03-configuration.md#35-secrets-encryption-appsettingsdataprotection).

---

Previous: [7. Connections and execution conditions](07-connections.md) · Next: [9. Events reference](09-events.md)
