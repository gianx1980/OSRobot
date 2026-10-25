# 12. Security and deployment

## 12.1 Trust model

Anyone who can log in to OS-Robot - or who can write `jobs.json` on disk - can run any code on the
server with the rights of the account the server runs under: a job can run any program, run C#
code, read environment variables and connect to any system it has credentials for. Treat access to
the Job Editor like shell access to the server.

The full explanation is in [SECURITY.md](../../SECURITY.md).

## 12.2 Checklist

- Change the default **admin** password (enforced at first login).
- Replace `JWT:Key` with a long random value.
- Run the server under a **dedicated, least-privilege account** - never `LocalSystem` or a domain
  admin.
- Keep the default `localhost` binding unless remote access is needed; if it is, put **HTTPS** in
  front (IIS, nginx/Caddy, or Kestrel with a certificate). The server logs a warning if it is
  exposed on the network without HTTPS.
- Give jobs database, FTP and SMTP accounts with only the permissions they need.
- If job authors are not fully trusted, set `ScriptingEnabled: false` **and**
  `RunProgramAllowedExecutablePaths` - the two settings work as a pair.
- Move passwords out of `jobs.json` into [secrets](08-dynamic-data.md#87-variables-and-secrets)
  (saving warns about those still in plain text), and protect `Data\jobs.json`, `Data\config.db`
  and `Data\keys`.
- On Linux, protect the secrets' keys with a certificate (see
  [DEPLOYMENT.md](../../OSRobot.Server/DEPLOYMENT.md)). Back up `Data\keys` together with
  `config.db`.
- Review `ExecLogs\audit-*.log` periodically.
- Behind a reverse proxy, exclude the `access_token` URL parameter from the proxy's access logs:
  the editor's notifications connection (`/hubs/notifications`) sends the login token that way,
  because browsers cannot add headers to a WebSocket.

## 12.3 Remote access options

| You already have | Use |
|---|---|
| IIS | Publish the server, host it under IIS with the ASP.NET Core Module, add an HTTPS binding. Omit the `Kestrel:Endpoints` section in `appsettings.Production.json`. |
| nginx, Caddy or another reverse proxy | Keep the server on `http://127.0.0.1:7098` and let the proxy terminate TLS. If the proxy is on another machine, set `ReverseProxy:KnownProxies` or `KnownNetworks`. |
| Nothing else | Add an HTTPS endpoint to Kestrel with a PFX file or a certificate from the Windows store. |

Full instructions are in [DEPLOYMENT.md](../../OSRobot.Server/DEPLOYMENT.md).

---

Previous: [11. Execution logs and monitoring](11-logs.md) · Next: [13. Troubleshooting](13-troubleshooting.md)
