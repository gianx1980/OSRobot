// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

namespace OSRobot.Server.Configuration;

public class JobEngineConfig : IJobEngineConfig
{
    public string LogPath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
    public bool SerialExecution { get; set; }
    public int MaxConcurrentTasks { get; set; } = 16;
    public int CleanUpLogsOlderThanHours { get; set; }
    public int CleanUpLogsIntervalHours { get; set; }
    public int StopDrainTimeoutSeconds { get; set; } = 30;
    public bool ScriptingEnabled { get; set; } = true;
    public string RunProgramAllowedExecutablePaths { get; set; } = string.Empty;
}

public class JWTConfig
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ExpireInMinutes { get; set; } = string.Empty;
    public int RequestNewTokenIfMinutesLeft { get; set; }
}

public class RefreshTokenConfig
{
    public int ExpireInMinutes { get; set; }
}

public class ClientSettingsConfig
{
    public string AppTitle { get; set; } = string.Empty;
    public string StaticFilesUrl { get; set; } = string.Empty;
    public int HeartBeatInterval { get; set; }
    public bool NotificationServerSentEventsEnabled { get; set; }
    public int NotificationPollingInterval { get; set; }
}
public class UserConfig
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}

/// <summary>
/// Trust settings for the ForwardedHeaders middleware, used only when OSRobot sits behind a
/// reverse proxy (IIS out-of-process, nginx, Caddy, ...) that terminates TLS and forwards
/// plain HTTP to Kestrel. Loopback (127.0.0.1/::1) is always trusted by default, which already
/// covers a proxy running on the same machine as OSRobot - the common case. Only fill these in
/// if the proxy runs on a different host. See DEPLOYMENT.md.
/// </summary>
public class ReverseProxyConfig
{
    /// <summary>Comma-separated IP addresses of trusted reverse proxies (e.g. "10.0.0.5,10.0.0.6").</summary>
    public string KnownProxies { get; set; } = string.Empty;

    /// <summary>Comma-separated trusted CIDR networks (e.g. "10.0.0.0/24").</summary>
    public string KnownNetworks { get; set; } = string.Empty;
}

/// <summary>Login lockout policy. See SECURITY.md.</summary>
public class SecurityConfig
{
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
}

/// <summary>
/// Where the keys that encrypt secrets are kept, and how they are protected themselves. See
/// DataProtectionSetup, SECURITY.md and DEPLOYMENT.md.
/// </summary>
public class DataProtectionConfig
{
    /// <summary>Folder of the key ring. Empty: a "keys" folder inside JobEngineConfig:DataPath. Back it up with config.db.</summary>
    public string KeysPath { get; set; } = string.Empty;

    /// <summary>
    /// A .pfx certificate (with its private key) that encrypts the key ring, on any platform. Empty: DPAPI
    /// on Windows, and on other systems the keys are protected by the folder's permissions only.
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the systemd credential (LoadCredential=/LoadCredentialEncrypted=) holding the certificate's
    /// password, read from $CREDENTIALS_DIRECTORY. Looked up before CertificatePasswordEnvironmentVariable.
    /// </summary>
    public string CertificatePasswordCredential { get; set; } = "dp-cert-password";

    /// <summary>Environment variable holding the certificate's password, when no systemd credential has it.</summary>
    public string CertificatePasswordEnvironmentVariable { get; set; } = "OSROBOT_DP_CERT_PASSWORD";

    /// <summary>
    /// Comma-separated .pfx files of certificates used before CertificatePath (same password), so the keys
    /// they encrypted can still be read after the certificate is replaced.
    /// </summary>
    public string PreviousCertificatePaths { get; set; } = string.Empty;
}

/// <summary>Limits of the calls to /hooks/. See WebhookEndpoint and DEPLOYMENT.md.</summary>
public class WebhooksConfig
{
    public long MaxBodyBytes { get; set; } = 1024 * 1024;

    /// <summary>All the calls from one address, accepted or not. Each hook also has its own limit.</summary>
    public int MaxRequestsPerMinutePerIp { get; set; } = 600;
}

public class AppSettings
{
    public JWTConfig JWT { get; private set; } = new JWTConfig();
    public RefreshTokenConfig RefreshToken { get; private set; } = new RefreshTokenConfig();
    public ClientSettingsConfig ClientSettings { get; private set; } = new ClientSettingsConfig();
    public JobEngineConfig JobEngineConfig { get; set; } = new JobEngineConfig();
    public ReverseProxyConfig ReverseProxy { get; private set; } = new ReverseProxyConfig();
    public SecurityConfig Security { get; private set; } = new SecurityConfig();
    public DataProtectionConfig DataProtection { get; private set; } = new DataProtectionConfig();
    public WebhooksConfig Webhooks { get; private set; } = new WebhooksConfig();
}
