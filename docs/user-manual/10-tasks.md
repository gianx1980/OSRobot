# 10. Tasks reference

All tasks share the General and Iterations sections (see
[Settings common to all objects](06-common-settings.md)) and the
[standard dynamic data fields](08-dynamic-data.md#84-standard-fields-of-every-object). Text
settings accept dynamic data unless stated otherwise. Unless noted, tasks run on all platforms.

- [10.1 File tasks](#101-file-tasks) - Read/Write text file, Read/Write binary file, File system, Excel file
- [10.2 Archives](#102-archives) - Zip, Unzip
- [10.3 SQL Server tasks](#103-sql-server-tasks) - Command, Bulk copy, File loader, Backup
- [10.4 Network tasks](#104-network-tasks) - FTP/SFTP, REST API, Ping, Send e-mail, Notification
- [10.5 Run program](#105-run-program)

## 10.1 File tasks

### Read text file

Reads rows from a text file into the `DefaultRecordset`.

| Setting | Description |
|---|---|
| File path | File to read. |
| Rows to read | **Read all the rows** (optionally **Skip first line**), **Read the last row**, **Read row number**, or **Read an interval**: from row to row, from row to last row, or the last N rows. |
| Columns split | **None** (one column per row), **Use delimiters** (Tab, Semicolon, Comma, Space or another character; optionally *Text is enclosed in double quotes*) or **Use fixed width columns**. |
| Columns definition | For each column: name, data type (**String**, **Integer**, **Decimal**, **Datetime**), expected format and culture, *Treat "NULL" string as null value*, *Is identity*, and the column number or start/end character positions. |

> [!TIP]
> Use **Select a file to parse to autodetect configuration** to let the editor propose delimiters
> and columns from a sample file.

### Write text file

Writes or edits a text file.

| Setting | Description |
|---|---|
| File path | File to write. |
| Action | **Append row**, **Insert row** (at a given row) or **Replace text** (find text / replace with text). |
| Columns definition | For each column: header title, field value (usually dynamic data) and field width. |
| Format | **Format as delimited file** (choose delimiters, optionally enclose text in double quotes) or **Format as fixed length column file**. |
| Add header if file is empty | Writes the header titles when the file is new or empty. |

> [!TIP]
> Combine with iterations: a Write text file task iterating over a recordset appends one line per
> row.

### Read binary file

Reads one or more files as binary content.

| Setting | Description |
|---|---|
| File path | File (or files) to read. |
| Recursive | Also read matching files in subfolders. |

**Dynamic data:** `DefaultRecordset` with columns `FullName`, `Name`, `Extension`, `Size`,
`CreationTime`, `LastAccessTime`, `LastWriteTime`, `FileContent`.

### Write binary file

| Setting | Description |
|---|---|
| File path | File to write. |
| File content source | A single dynamic data reference to binary data, e.g. `{object[7].DefaultRecordset['FileContent']}`. |

### File system

Performs file and folder operations. Choose the command:

| Command | Settings |
|---|---|
| Copy | List of copy items: source path, destination path, only files older than N days/hours/minutes, **Overwrite file if exists**, **Recursively copy**. |
| Delete | List of paths: path, only files older than N days/hours/minutes, **Recursively delete**. |
| Create folder | Path of the folder to create. |
| Check existence | Path to check. Sets `FilePathExists`. |
| List | Folder to list, **List files**, **List folders**, **List subfolders content**. Returns `DefaultRecordset` with `FullName`, `Name`, `Extension`, `Size`, `CreationTime`, `LastAccessTime`, `LastWriteTime`, `IsDirectory`. |
| Rename | Rename from / rename to. |

### Excel file

Reads and edits `.xlsx` workbooks.

| Setting | Description |
|---|---|
| File path / Sheet name | Workbook and worksheet to use. |
| Action | **Append row**, **Insert row**, **Read row**, **Delete row**, **Find first row**, **Find all rows**, **Find and replace**. |
| Columns definition | For writing actions: header title and cell value of each column. *Add header if file is empty* writes the titles in a new sheet. |
| Read options | Last row, a row number, or an interval (from/to, from row to last row, last N rows); number of columns to read (empty = all). |
| Find text / Replace with | For the find actions. |

**Dynamic data:** `DefaultRecordset` with columns `Column1`, `Column2`, ...

## 10.2 Archives

### Zip

| Setting | Description |
|---|---|
| Source | File or folder to compress. |
| Destination archive | Archive to create. |
| Options | **Include files in subfolders**, **Store full path**, **Skip empty folders**. |
| If destination archive exists | **Add / overwrite files in the archive**, **Create with unique names** or **Fail**. |
| Compression level | **Low**, **Medium** or **High**. |

### Unzip

| Setting | Description |
|---|---|
| Source archive | Archive to extract. |
| Destination folder | Where to extract. |
| If destination file exists | **Overwrite**, **Create with unique names** or **Fail**. |

## 10.3 SQL Server tasks

All SQL Server tasks share a connection section: **Server**, **Database**, **Username**,
**Password**, **Connection string options** (extra parameters appended to the connection string,
e.g. `TrustServerCertificate=True`) and **Command timeout (seconds)** (default 30). Use **Test
connection** to verify the parameters.

### SQL Server command

| Setting | Description |
|---|---|
| Type | **Text** (a SQL statement) or **Stored procedure**. |
| Query | Statement or procedure name. Can contain dynamic data. |
| Parameters | For each parameter: name, value, data type (Varchar, NVarchar, Xml, Numeric, Int, Long, Date, Datetime, Bit, Varbinary...), length and precision/scale. *Pass NULL value* sends a database NULL. |
| Returns recordset | Turn on for queries that return rows: they become the `DefaultRecordset`. |

> [!TIP]
> Prefer parameters over putting dynamic data directly in the SQL text: they avoid quoting
> problems and SQL injection.

### SQL Server bulk copy

| Setting | Description |
|---|---|
| Source recordset | Dynamic data reference to a recordset, e.g. `{object[4].DefaultRecordset}`. |
| Destination table | Table or Schema.Table, e.g. `dbo.Customers`. It must already exist. |

### SQL Server file loader

Loads a delimited text file or an Excel `.xlsx` file into a table, creating the table if needed.

| Setting | Description |
|---|---|
| File path | A delimited text file (separator detected automatically) or an `.xlsx` file. |
| Autodetect separator / Separator | Text files only. Write `\t` for a tab. |
| Autodetect encoding / Encoding | Text files only, e.g. `utf-8`, `windows-1252`. A byte order mark at the start of the file always wins. |
| Sheet name | Excel files only. Empty: the first sheet. |
| Destination table | `MyTable`, `dbo.MyTable` or `[My schema].[My table]`. |
| First row has column names | Use the first row as column names. |
| Autodetect column types | When off, every column is created as text. |
| Culture of numbers and dates | e.g. `it-IT`, `en-US`. Empty: the server's regional settings. |
| If the table exists | **Fail**, **Append the rows** (columns matched by name) or **Drop and recreate it**. |

**Dynamic data:** `RowsLoaded`, `TableName`, `TableCreated`.

### SQL Server backup

| Setting | Description |
|---|---|
| Backup type | **Full** or **Transaction log**. |
| Databases to backup | **All databases**, **All user databases** or **Selected databases**. |
| Destination path / File name template | Folder and file name of the backup files (supports dynamic data, e.g. the date). The path is seen from the SQL Server machine. |
| Options | **Overwrite file if exists**, **Verify backup**, **Perform checksum**, **Continue backup if checksum error occurs**. |
| Compression | **Use server default**, **Compress backup** or **Do not compress backup**. |

**Dynamic data:** `SuccessfulBackupsNumber`, `FailedBackupsNumber`.

## 10.4 Network tasks

### FTP / SFTP

| Setting | Description |
|---|---|
| Protocol | **FTP** or **SFTP**. |
| Host / Port / Username / Password | Server and credentials. |
| Command | **Copy**: list of items with direction (**Local to remote** or **Remote to local**), local path, remote path, *Overwrite file if exists*, *Recursively copy folders*. **Delete**: list of remote paths. |

### REST API

| Setting | Description |
|---|---|
| URL | Address to call. Supports dynamic data. |
| Method | **GET**, **POST**, **PUT** or **DELETE**. |
| Headers | Name/value pairs, e.g. `Authorization`, `Content-Type`. |
| Body | Request body for POST/PUT. |
| JSON Path to data | JSONPath expression that extracts a value or array from the response. |
| Returns recordset | Turn the extracted JSON array into the `DefaultRecordset`. |

**Dynamic data:** `URL`, `RawContent` (response body), `HttpResult` (HTTP status), `JsonPathData`,
`DefaultRecordset`.

### Ping

| Setting | Description |
|---|---|
| Host | Host name or IP address. |
| Timeout (ms) | Wait time for each reply. |
| Attempts | Number of pings to send. |

**Dynamic data:** `ThresholdSuccessRate` - percentage of attempts that got a reply. Use it in a
connection condition (see [Execute and don't-execute conditions](07-connections.md#73-execute-and-dont-execute-conditions)).

### Send e-mail

| Setting | Description |
|---|---|
| Recipients / CC / BCC | Lists of addresses. |
| Sender, Subject, Message | Message content; all accept dynamic data. **Send body as HTML** sends the message as HTML. |
| Attachments | List of files to attach. |
| SMTP server / Port | Mail server (default port 25). |
| Connection security | **Automatic (recommended)**, **None (unencrypted)**, **STARTTLS (required)**, **STARTTLS (if available)** or **Implicit TLS / SSL (usually port 465)**. |
| Authenticate | Turn on and enter Username and Password if the server requires login. |

### Notification

Shows a short message to everyone who has the job editor open, without e-mail: a desktop
notification when the editor's browser tab is in the background or minimized, a notification
inside the editor when it is in front.

| Setting | Description |
|---|---|
| Title | Required. Up to 200 characters are sent; longer text is cut. |
| Message | Optional body text, up to 2,000 characters. |
| Level | **Information**, **Success**, **Warning** or **Error**: sets the colour and icon. Error notifications inside the editor stay until closed; the others close after 8 seconds. |

One notification is sent per iteration. By default the task iterates the default recordset of the
previous task: one notification per row, or a single one when the previous object has no default
recordset (an event, or a task that doesn't produce one, such as Ping). A recordset that is present
but empty, for example a query that returned no rows, sends nothing. Title and message accept dynamic data, for example
`Backup of {object[3].Name} failed`; a title that ends up empty makes that iteration fail.

Things to know:

- **An editor must be open to receive it.** A background tab or a minimized window is enough.
  Notifications sent while no editor is open are not kept, and nothing is retried. The task
  still succeeds: it only fails if the server could not send the notification at all.
- **Desktop notifications must be allowed**, once per browser: open the user menu (person icon)
  and click **Desktop notifications**. Clicking it again turns them off and on for that browser
  only. Notifications inside the editor are always shown.
- **Browsers only allow desktop notifications on HTTPS or on the server itself**
  (`http://localhost`). When the editor is opened over plain HTTP from another computer, the menu
  item is disabled and only notifications inside the editor are shown.
- Everyone signed in to the editor receives every notification.

## 10.5 Run program

Starts an external program and waits for it to finish.

| Setting | Description |
|---|---|
| Program path | Executable to run. |
| Parameters | Command-line arguments (supports dynamic data). |
| Working folder | Folder the program starts in. |
| Fail if the program exits with a non-zero code | On by default: a non-zero exit code makes the task fail, so *Object does not execute* conditions can react to it. Turn it off for programs that use non-zero codes for success. |

If the task is cancelled (for example when the server stops), the program and its child processes
are terminated. When `RunProgramAllowedExecutablePaths` is set, only the listed executables or
folders can be started.

---

Previous: [9. Events reference](09-events.md) · Next: [11. Execution logs and monitoring](11-logs.md)
