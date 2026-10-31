# Security model and trust boundary

## The trust boundary, stated plainly

**Anyone who can authenticate to the OSRobot Server API — or who can write to `jobs.json` on
disk — can execute arbitrary code as the account OSRobot Server runs under.**

This is not an accidental gap to be patched; it is what the product does. A job can:

- Run any executable on the host, with any arguments (`RunProgramTask`).
- Run arbitrary inline C# via `[CODE]` dynamic-data expressions, with full access to the .NET
  BCL — file system, network, process creation, reflection, everything (`DynamicDataParser`).
- Read any environment variable the service process can see (`{environment['VAR']}`), and use
  any stored variable or secret (`{var['Name']}`, `{secret['Name']}`).
- Connect to any database, FTP/SFTP server, or SMTP server it's given credentials for.

None of this is sandboxed. There is no capability restriction between "what a job is allowed to
do" and "what the OS account running OSRobot Server is allowed to do." **Treat access to the Job
Editor API exactly as you would treat interactive shell access to the host it runs on** — because
functionally, that's what it is.

Optional, off-by-default configuration knobs exist to *narrow* this for deployments that don't
need the full power (see "Reducing the surface" below), but the product's default, supported mode
is full power for anyone who can log in. Don't rely on the UI alone to enforce anything a
sufficiently motivated authenticated user couldn't route around via a `[CODE]` block.

## What actually stands between a stranger and that power

Since the capability itself isn't sandboxed, everything protective has to happen at the boundary
around it:

1. **Network exposure.** By default OSRobot Server only listens on `http://localhost:7098` —
   unreachable from anywhere but the host itself. If you expose it further, do it behind TLS (see
   [OSRobot.Server/DEPLOYMENT.md](OSRobot.Server/DEPLOYMENT.md)). Without TLS, the JWT bearer
   token used for every authenticated request travels in plaintext, and capturing it is
   equivalent to capturing the password.
   - One exception to "the token travels in a header": browsers cannot set headers on a
     WebSocket, so the editor's notifications connection (`/hubs/notifications`, used by the
     Notification task) sends the JWT as an `access_token` query parameter. The server accepts a
     token from the URL for that path only. OSRobot's own log doesn't record request URLs:
     `Program.cs` forces ASP.NET's request logging (`Microsoft.AspNetCore.Hosting.Diagnostics`)
     to `Warning` after reading the Serilog configuration, so no `appsettings.json` can turn it
     back on (the `Logging:LogLevel` section never applied to the Serilog file anyway).
     **Reverse proxies (IIS, nginx) usually log full URLs**: exclude the `access_token` parameter
     from their access logs, or treat those logs as sensitive. Tokens are short-lived, which limits
     what a leaked log line is worth.
   - The notifications hub refuses users who still have to change the default password, the
     same way `MustChangePasswordFilter` blocks the API.
2. **Authentication.** A single admin account, PBKDF2-hashed password, JWT-based sessions.
   - Change the default `admin` credentials before doing anything else — the app enforces this on
     first login (`MustChangePassword`).
   - Repeated failed logins lock the account out temporarily (`AppSettings:Security`).
   - All login attempts, lockouts, job-configuration saves, and manual task starts are written to
     a dedicated audit log (`ExecLogs/audit-*.log`) with the authenticated username, so exercise
     of this power is never anonymous.
3. **The OS account OSRobot Server runs as.** This is the real ceiling on the damage a malicious
   or compromised job can do, since the product itself imposes none. Run the Windows Service
   under a dedicated account with only the file-system, network, and database permissions your
   actual jobs need — never `LocalSystem` or a domain administrator account. See
   [OSRobot.Server/DEPLOYMENT.md](OSRobot.Server/DEPLOYMENT.md) for guidance. A compromised job
   editor on a least-privilege account is a contained incident; the same compromise running as
   `LocalSystem` is a full host takeover.

## Secrets and variables

Credentials don't have to sit in `jobs.json`: a job can write `{secret['Name']}` and the value is
kept in `config.db`, encrypted with ASP.NET Core Data Protection.

- **Keys.** The Data Protection key ring lives in its own folder (`Data/keys`,
  `AppSettings:DataProtection:KeysPath`), never in `config.db`. The keys are encrypted with a
  certificate when `AppSettings:DataProtection:CertificatePath` is set, otherwise with DPAPI on
  Windows (tied to the service account and machine), otherwise not at all: on Linux/macOS without
  a certificate only the folder's permissions protect them, and the server logs a warning at
  startup. A configured certificate that can't be opened stops the server instead of falling back.
  Data Protection never re-encrypts an existing key, so at each start `SecretKeyRotation` checks the
  key in use against the configuration and, if they differ, creates a new key and re-encrypts every
  secret with it: changing the protection takes effect at once, not when the old key expires.
  On Linux, deliver the certificate's password as a TPM-bound systemd credential (see
  [OSRobot.Server/DEPLOYMENT.md](OSRobot.Server/DEPLOYMENT.md)).
- **What this protects against:** a copied or leaked `config.db`, `jobs.json` or backup of
  either, and anyone looking at the editor. A secret's value never leaves the server: the API
  can set it but never returns it, and the SQL Server *Test connection* endpoints resolve
  placeholders on the server and take the connection details in a POST body, so no password ends
  up in a URL or a proxy log.
- **What it does not protect against:** anyone who can edit jobs. They can use every secret,
  including sending it to a server of their own with a REST API task. And anyone who can run code
  as the service account can decrypt the keys, since that's how the engine reads them. Secrets are
  not access control between job authors.
- **Redaction.** Every secret value of 4 characters or more is replaced with `***` in execution
  logs, in the messages of the application log and in Notification task messages. It is a safety
  net: a transformed secret (encoded, split, truncated) isn't recognized, and exception details in
  the application log are written as they are.
- **Single-pass resolution.** Placeholders are found in the configured text and replaced in one
  pass, so data coming from outside (a file, a REST response, an e-mail) that contains
  `{secret['X']}` or `{environment['X']}` is never expanded.
- **Audit.** Creating, changing and deleting a variable or secret is written to the audit log, by
  name only.
- **Plain-text passwords.** Saving the jobs warns about every `Password` setting written as plain
  text, and the editor offers to move it to a secret. `jobs.json.bak` keeps the previous version
  of the jobs until the next save.

## Webhooks

The Webhook event is the only way to start a job without logging in: `POST /hooks/{path}` is anonymous,
and the hook itself checks a token or an HMAC signature, in constant time.

- **Exposure.** By default the hooks answer on the same loopback endpoint as everything else. A Kestrel
  endpoint named `Webhooks` opens them to other machines: that endpoint serves `POST /hooks/...` and
  answers 404 to anything else, ahead of the rest of the pipeline, so the editor, login, API, hub and
  Swagger don't exist there, and anything added later stays private by default. The restriction follows from
  the endpoint's name, so it can't be forgotten; a URL it can't parse stops the server.
- **Probing.** A wrong credential, an unknown path and a disabled hook get the same 404. Wrong credentials
  are written to the audit log.
- **Flooding.** A body limit, a limit per address and one per hook. The hook's limit also protects the task
  queue, which is unbounded. Rejected calls don't create log files.
- **The body is untrusted.** It is never parsed for placeholders, so it can't read a secret or an
  environment variable, and it is never logged. But it is data controlled by the caller: a job that pastes it
  into SQL text or program arguments is open to injection. Credential headers don't reach the job's data.
- **What a credential allows.** Whoever has it can start that job, as often as the limits allow, with a body of
  their choosing. A signature doesn't expire, so a recorded call can be replayed. Use HTTPS: without it the
  token and the body can be read on the network.

## Reducing the surface (optional, off by default)

If a specific deployment doesn't need the full power — for example, an environment with multiple
job authors who don't fully trust each other, or one that simply never uses C# scripting — two
opt-in settings under `AppSettings:JobEngineConfig` narrow things without affecting anyone who
leaves them at their defaults:

- `ScriptingEnabled` (default `true`) — set to `false` to make every `[CODE]` dynamic-data
  expression fail validation instead of executing.
- `RunProgramAllowedExecutablePaths` (default empty = unrestricted) — a comma-separated allowlist
  of executable paths or directories `RunProgramTask` may launch.

**Important:** `RunProgramAllowedExecutablePaths` only constrains the `RunProgramTask` plugin
specifically. It does **not** stop a `[CODE]` script from calling `System.Diagnostics.Process`
directly — if you want the executable allowlist to mean anything, set `ScriptingEnabled: false`
too. The two settings are a matched pair, not independent options.

## Reporting a vulnerability

If you find a security issue that isn't the inherent trust model described above (e.g., an
authentication bypass, a way to reach job-editing power *without* a valid credential, or a way to
escape the documented trust boundary itself), please report it privately rather than opening a
public issue.
