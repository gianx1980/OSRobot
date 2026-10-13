# 7. Connections and execution conditions

A connection decides whether the target task runs after its source, how long to wait first and
how many times to run it.

## 7.1 General settings

| Setting | Description |
|---|---|
| Wait seconds before execute next object | Delay before the target task is queued. The wait does not block other tasks. |
| Run next object | Only when the source is a task: **Once for each result (each iteration)** or **Once, after all iterations, with all the results** (see 7.2). |
| The collected result is successful when | Only with *Once, after all iterations*: **All iterations succeeded** or **At least one iteration succeeded**. Decides whether the collected result counts as "executed". |
| Enabled | Disabled connections are ignored. |

## 7.2 Run mode with multiple iterations

When the source task ran several iterations it produced several results, one per iteration.

- **Once for each result** - the conditions are evaluated for every iteration's result, and the
  target runs once for each result that satisfies them. A task with 10 iterations can start its
  successor 10 times.
- **Once, after all iterations, with all the results** - the results are merged into one:
  recordsets (such as the default recordset) contain the rows of all the iterations, other fields
  hold the last iteration's values, the execution dates cover the whole execution, and an extra
  `IterationResults` recordset holds each iteration's data. The conditions are evaluated once and
  the target runs at most once. Typical use: send a single summary e-mail after processing many
  files.

## 7.3 Execute and don't-execute conditions

Each connection has two lists of conditions, both evaluated against the result of the source
object:

1. If **any** *don't execute* condition is true, the target does **not** run.
2. Otherwise, if **any** *execute* condition is true, the target runs.
3. Otherwise the target does not run.

> [!WARNING]
> A connection with an empty *execute* list never runs its target. Keep at least one execute
> condition (the default *Object executes* is usually what you want).

Use **Add** to create a condition and the edit menu of each row to edit, delete or reorder it. For
value conditions you pick a dynamic data field of the source object from the list, choose the
operator and enter the value.

| Operator | True when |
|---|---|
| Object executes | The source completed successfully. |
| Object does not execute | The source failed. |
| Value equals to | The field, as text, is exactly equal to the value. |
| Value greater than / Value less than | The field and the value are numbers and the comparison holds. |
| Value between | The field is a number between Min value and Max value (inclusive). |
| Value contains / starts with / ends with | Text comparison, case-sensitive. |

> [!NOTE]
> Numeric values in conditions are written with a dot as decimal separator (e.g. `85.5`), whatever
> the server's regional settings. A field that is not a number never satisfies a numeric
> condition.

### Examples

- Run only when a CPU event reports more than 90%: execute condition on `CpuUsagePercentage`,
  *Value greater than* `90`.
- Skip `.tmp` files from a folder watcher: don't-execute condition on `FileName`, *Value ends
  with* `.tmp`.
- Alert when a host does not answer: from a Ping task, execute condition on
  `ThresholdSuccessRate`, *Value less than* `50`.

---

Previous: [6. Settings common to all objects](06-common-settings.md) · Next: [8. Dynamic data](08-dynamic-data.md)
