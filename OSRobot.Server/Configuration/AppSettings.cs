// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

namespace OSRobot.Server.Configuration;

public class JobEngineConfig : IJobEngineConfig
{
    public string LogPath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
    public bool SerialExecution { get; set; }
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

public class AppSettings
{
    public JWTConfig JWT { get; private set; } = new JWTConfig();
    public RefreshTokenConfig RefreshToken { get; private set; } = new RefreshTokenConfig();
    public ClientSettingsConfig ClientSettings { get; private set; } = new ClientSettingsConfig();
    public JobEngineConfig JobEngineConfig { get; set; } = new JobEngineConfig();
    public ReverseProxyConfig ReverseProxy { get; private set; } = new ReverseProxyConfig();
    public SecurityConfig Security { get; private set; } = new SecurityConfig();
}
