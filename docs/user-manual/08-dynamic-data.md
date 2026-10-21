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
| `[CODE]...` | The whole setting is a C# expression (see 8.6). |

Examples:

```text
C:\Backup\{object[1].ExecutionStartDateUnderscoreDate}\report.csv
{object[5].DefaultRecordset['CustomerName']}
{object[5].DefaultRecordset[0]['Total']}
{environment['TEMP']}\work
```

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

---

Previous: [7. Connections and execution conditions](07-connections.md) · Next: [9. Events reference](09-events.md)
