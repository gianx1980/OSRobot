# Security model and trust boundary

## The trust boundary, stated plainly

**Anyone who can authenticate to the OSRobot Server API — or who can write to `jobs.json` on
disk — can execute arbitrary code as the account OSRobot Server runs under.**

This is not an accidental gap to be patched; it is what the product does. A job can:

- Run any executable on the host, with any arguments (`RunProgramTask`).
- Run arbitrary inline C# via `[CODE]` dynamic-data expressions, with full access to the .NET
  BCL — file system, network, process creation, reflection, everything (`DynamicDataParser`).
- Read any environment variable the service process can see (`{environment['VAR']}`).
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
