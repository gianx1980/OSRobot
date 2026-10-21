# 6. Settings common to all objects

## 6.1 General

| Setting | Description |
|---|---|
| Object Id | Read-only unique number of the object, used in dynamic data references. |
| Name | Descriptive name (mandatory, up to 500 characters). Available at run time as the `ObjectName` dynamic data. |
| Enabled | Turns the event or task on or off (not shown for folders). |
| Log | Writes the object's activity to the execution log (not shown for folders). |

## 6.2 Iterations (tasks)

Most tasks have an **Iterations** section that decides how many times the task runs each time it
is started:

| Option | Behaviour |
|---|---|
| Iterate as many times as the records in the default recordset of the previous task | The default. If the task that started this one produced a `DefaultRecordset` with 10 rows, this task runs 10 times. If there is no default recordset it runs once. |
| Iterate as many times as the records contained here | Runs once per row of a specific recordset, written as a dynamic data reference such as `{object[4].DefaultRecordset}`. Use the bolt button to pick it. The task fails if that recordset is not available. |
| Iterate this exact number of times | Runs exactly the number of times you enter. |

In every iteration, references to recordset fields without an explicit row number automatically
point to the row of the current iteration (see
[Recordsets and iterations](08-dynamic-data.md#83-recordsets-and-iterations)). This is how you
process a file or a query result row by row without writing a loop.

Each task also produces the `NumberOfIterations` dynamic data, and - when it ran several
iterations - its results can be passed on once per iteration or all together (see
[Run mode with multiple iterations](07-connections.md#72-run-mode-with-multiple-iterations)).

---

Previous: [5. Building jobs](05-building-jobs.md) · Next: [7. Connections and execution conditions](07-connections.md)
