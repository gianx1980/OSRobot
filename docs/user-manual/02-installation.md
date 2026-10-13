# 2. Installation and first start

## 2.1 Requirements

- **Server:** the .NET 10 runtime (the .NET 10 SDK if you build from source). Windows, Linux and
  macOS are supported; a few plugins are Windows-only (see the *Platforms* line of each plugin in
  [Events](09-events.md) and [Tasks](10-tasks.md)).
- **Job Editor:** any modern browser. Node.js and npm/yarn are needed only to build or run the
  editor from source.
- **Optional:** access to the SQL Server instances, SMTP servers, FTP/SFTP servers and APIs your
  jobs will use.

## 2.2 Starting the server

From the source code, run:

```bash
cd OSRobot.Server
dotnet run
```

From a published build, start the `OSRobot.Server` executable in its folder. On first start the
server:

- creates its SQLite configuration database with a default account;
- creates an empty `Data\jobs.json`;
- listens on `http://localhost:7098` - reachable from the same machine only, which is the safe
  default.

> [!NOTE]
> You can open `http://localhost:7098/swagger` to explore the REST API used by the Job Editor.

## 2.3 Starting the Job Editor

During development the editor is started with the Quasar CLI and connects to the running server:

```bash
cd OSRobot.JobEditor
npm install
quasar dev
```

For production, build the editor (see [OSRobot.JobEditor/README.md](../../OSRobot.JobEditor/README.md))
and publish it on a web server, or serve it next to the API.

## 2.4 First login

1. Open the Job Editor in your browser.
2. Log in with the default account: user name **admin**, password **admin**.
3. The editor immediately asks you to change the password. Until you do, no other operation is
   allowed.
4. Enter the current password, the new password and its confirmation, then confirm.

> [!WARNING]
> Change the default password before doing anything else, and change the JWT `Key` in
> `appsettings.json` (see [Server configuration](03-configuration.md)) on every installation: the
> value shipped in the repository is public.

After several failed login attempts the account is temporarily locked (5 attempts and 15 minutes
by default, see `AppSettings:Security`). The editor shows *"Your account has been temporarily
locked out..."*; wait for the lockout to expire and try again.

Sessions use short-lived tokens that are renewed automatically. If the session expires (for
example after a long inactivity), the editor shows *"Your session has expired, please login
again"*.

## 2.5 Running as a Windows Service

On Windows the server can run as a service so that jobs keep running without a logged-on user.
Register the published executable with the Services manager or `sc.exe`, then configure the
account it runs under:

```bash
sc.exe config OSRobotServer obj= ".\OSRobotSvc" password= "..."
```

> [!WARNING]
> Do not run the service as `LocalSystem` or as a domain administrator. A job can run any program,
> so the service account is the only real limit on what a job can do. Create a dedicated account
> with access only to the folders, databases and servers your jobs need (see
> [Security and deployment](12-security.md)).

---

Previous: [1. Introduction](01-introduction.md) · Next: [3. Server configuration](03-configuration.md)
