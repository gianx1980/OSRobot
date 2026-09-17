# Deploying OSRobot Server beyond localhost

Out of the box, OSRobot Server listens on `http://localhost:7098` only. That's deliberately
safe: nothing outside the machine it runs on can reach it, so plaintext HTTP is fine — the
traffic never leaves the loopback interface.

**If you never rebind that address, you don't need anything in this document.**

If you want the web UI reachable from other machines on your LAN, or from the internet, put TLS
in front of it using **one** of the three options below. Pick based on what you already run:

| You already have... | Use |
|---|---|
| IIS | Option A |
| nginx, Caddy, or another reverse proxy | Option B |
| Nothing else, want Kestrel to terminate TLS itself | Option C |

Whichever you pick, `OSRobot.Server` will log a startup warning if it ever ends up bound
directly to a non-loopback address (a real LAN/external IP, not `localhost`/`127.0.0.1`) with no
HTTPS configured anywhere — that combination means login tokens and all traffic travel in
plaintext, and should only ever happen by mistake.

---

## Option A — IIS (reverse proxy)

IIS terminates TLS and forwards plain HTTP to OSRobot's Kestrel process over loopback via the
ASP.NET Core Module (ANCM), "out-of-process" hosting model.

1. Install the **.NET Hosting Bundle** (includes ANCM v2) on the Windows machine running IIS.
2. Publish the app: `dotnet publish -c Release -o <publish-folder>`. This generates a
   `web.config` automatically — you don't need to hand-write one.
3. In an `appsettings.Production.json` next to the published app, **omit the `Kestrel:Endpoints`
   section** (don't copy it from `appsettings.json`). OSRobot's own fixed
   `Kestrel:Endpoints:MyHttpEndpoint` binding would otherwise override the dynamic port ANCM
   negotiates with the app at startup — omitting it lets IIS/ANCM manage the backend port the
   normal way.
4. In IIS Manager, create a Site (or Application under an existing site) pointing at the publish
   folder, running under the ASP.NET Core Module.
5. Add an HTTPS binding on that site with a real certificate: Site → *Bindings* → *Add* → type
   `https`, select your certificate (or `New-WebBinding -Name "OSRobot" -Protocol https -Port 443
   -SslFlags 1` in PowerShell for SNI, or `netsh http add sslcert` for a specific IP:port).
6. That's it — IIS holds the certificate, OSRobot never sees plaintext-vs-TLS itself, and the
   `UseForwardedHeaders()` middleware already in the pipeline makes the app correctly see
   `https` as the scheme for redirect/JWT-issuer purposes.
7. IIS on the *same* machine as OSRobot needs no extra config — loopback is trusted by default.
   Only fill in `AppSettings:ReverseProxy:KnownProxies` (see Option B) if IIS runs elsewhere.

## Option B — nginx / Caddy (reverse proxy)

Keep OSRobot listening on loopback only and let the proxy hold the certificate.

1. In `appsettings.Production.json`, keep (or make explicit) a loopback-only Kestrel endpoint:
   ```json
   { "Kestrel": { "Endpoints": { "MyHttpEndpoint": { "Url": "http://127.0.0.1:7098" } } } }
   ```
2. Run OSRobot as the Windows Service, as usual — it's still only reachable from the same
   machine.
3. Point your reverse proxy at it. Example nginx server block:
   ```nginx
   server {
       listen 443 ssl;
       server_name osrobot.example.com;

       ssl_certificate     /etc/nginx/certs/osrobot.crt;
       ssl_certificate_key /etc/nginx/certs/osrobot.key;

       location / {
           proxy_pass         http://127.0.0.1:7098;
           proxy_set_header   Host              $host;
           proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
           proxy_set_header   X-Forwarded-Proto $scheme;
       }
   }
   ```
   Caddy is simpler still (it manages certificates automatically):
   ```caddyfile
   osrobot.example.com {
       reverse_proxy 127.0.0.1:7098
   }
   ```
4. If the proxy runs on the **same machine** as OSRobot (the common case), nothing else is
   needed — loopback is trusted by default.
5. If the proxy runs on a **different machine**, add its address to
   `AppSettings:ReverseProxy:KnownProxies` (comma-separated IPs) or `KnownNetworks`
   (comma-separated CIDR ranges, e.g. `10.0.0.0/24`) in `appsettings.Production.json`. Without
   this, forwarded headers from an untrusted source are ignored, and OSRobot will treat every
   request as plain HTTP (`UseHttpsRedirection` will keep trying, uselessly, to redirect
   requests that already arrived as HTTPS at the proxy).

## Option C — Direct Kestrel HTTPS (no reverse proxy)

Kestrel terminates TLS itself. Use this if you don't want to run IIS or a separate proxy.

1. Get a certificate — from a CA for a public hostname, or a self-signed one for LAN-only/internal
   use — as a PFX file, or import it into the Windows certificate store.
2. Add an HTTPS endpoint in `appsettings.Production.json`, alongside the existing HTTP one:
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "MyHttpEndpoint": { "Url": "http://localhost:7098" },
         "MyHttpsEndpoint": {
           "Url": "https://0.0.0.0:7099",
           "Certificate": {
             "Path": "C:\\path\\to\\osrobot.pfx",
             "Password": "your-pfx-password"
           }
         }
       }
     }
   }
   ```
   Or, to avoid keeping a PFX password in a config file, reference a certificate already
   installed in the Windows certificate store instead:
   ```json
   "MyHttpsEndpoint": {
     "Url": "https://0.0.0.0:7099",
     "Certificate": {
       "Subject": "osrobot.example.com",
       "Store": "My",
       "Location": "LocalMachine",
       "AllowInvalid": false
     }
   }
   ```
3. Update `AppSettings:ClientSettings:StaticFilesUrl` (and wherever the JobEditor frontend points
   at the API) to the new `https://...:7099` address.
4. Once an HTTPS endpoint exists, `UseHttpsRedirection()` (already in the pipeline) actively
   redirects plain HTTP requests to it — including on the existing `MyHttpEndpoint`, so you can
   leave that bound to `localhost` for local/diagnostic access or rebind it to `0.0.0.0` if you
   want HTTP-with-redirect available on the LAN too.

---

## If you must expose OSRobot without any of the above

This only makes sense on a fully trusted, isolated network (never the open internet). Rebinding
`MyHttpEndpoint`'s `Url` to `0.0.0.0` or a specific LAN IP, with no HTTPS endpoint and no reverse
proxy in front, means every request — including the JWT bearer token on every API call — travels
as plaintext to anyone who can observe that network segment. OSRobot will log a warning at
startup if it detects exactly this configuration.
