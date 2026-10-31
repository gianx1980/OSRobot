# Appendix A. Worked examples

## A.1 Nightly database backup with notification

1. Create a folder **Backups** and open it.
2. Add a **Date/Time event** (Id 1): *Every day at* 02:00:00, *All days*.
3. Add a **SQL Server backup** task (Id 2): connection to your instance, *Full*, *All user
   databases*, destination `D:\SqlBackups`, file name template using
   `{object[1].ExecutionStartDateUnderscoreDate}`, *Verify backup* on.
4. Connect the event to the backup task.
5. Add a **Send e-mail** task "Backup OK" and connect the backup task to it (default condition
   *Object executes*). Subject: `Backup OK - {object[2].SuccessfulBackupsNumber} databases`.
6. Add a second **Send e-mail** task "Backup FAILED" and connect the backup task to it; change the
   execute condition to *Object does not execute*. Message:
   `Failed backups: {object[2].FailedBackupsNumber}`.
7. Click **Save**, then **Reload server configuration**.

## A.2 Import incoming CSV files into SQL Server

1. Add a **File system event** (Id 1) watching `D:\Inbox` for *New files*.
2. Add a **SQL Server file loader** task (Id 2): file path `{object[1].FullPathName}`, destination
   table `dbo.Import`, *If the table exists*: *Append the rows*.
3. Click the connection from the event and replace its execute condition with `FileName` *Value
   ends with* `.csv`, so only CSV files are processed.
4. Add a **File system** task (Id 3) with command *Delete* on `{object[1].FullPathName}`, connected
   from the loader with *Object executes*.
5. Add a **Send e-mail** task connected with *Object does not execute* to report files that could
   not be loaded.

## A.3 Process the rows of a file one by one

1. **Read text file** (Id 2) with delimiters and a column definition, e.g. columns `Host` and
   `Owner`.
2. **Ping** task connected to it, Host = `{object[2].DefaultRecordset['Host']}`, iterations: *as
   many times as the records in the default recordset of the previous task*.
3. **Send e-mail** connected from the Ping task, *Once for each result*, execute condition
   `ThresholdSuccessRate` *Value less than* `50`, recipient
   `{object[2].DefaultRecordset[{subInstanceIndex}]['Owner']}`.

## A.4 One e-mail for every failure in a folder

1. In the folder whose jobs you want to watch (for example **Backups**), add a **Task failure
   event** (Id 9): *Watch*: *This folder and its subfolders*, *Group failures for* `60` seconds.
2. Add a **Send e-mail** task connected to it. Subject:
   `{object[9].FailureCount} failure(s) in {object[9].FailedObjectFolder}`. Message:
   `First: {object[9].FailedObjectName} - {object[9].FailureMessage}`.
3. Store the SMTP password as a secret and write `{secret['SmtpPassword']}` in the password field.
4. Click **Save**, then **Reload server configuration**.

From now on every task failure in the folder is reported, at most one e-mail a minute. To leave
out a task that already sends its own failure e-mail, add a *don't execute* condition to the
connection from the event: `FailedObjectName` *Value equals to* the task's name.

## A.5 Start a job from another system

1. Add a **Webhook event** (Id 9). Set the path to `build-done` and click **Generate token**. Copy
   the token shown: it won't be shown again.
2. Set *JSON Path to data* to `$.version`, so the job receives the version the caller sends.
3. Connect the event to the tasks to run, using `{object[9].JsonPathData}` in their settings.
4. Click **Save**, then **Reload server configuration**.
5. Have the other system call it:

   ```bash
   curl -X POST -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{\"version\": \"1.4.0\"}" http://localhost:7098/hooks/build-done
   ```

---

Previous: [13. Troubleshooting](13-troubleshooting.md) · Next: [Appendix B. Quick reference](appendix-b-quick-reference.md)
