/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OSRobot.Server;
using OSRobot.Server.Configuration;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Infrastructure.Hosting;
using OSRobot.Server.Infrastructure.Security;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.JobEngineLib;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using Serilog;
using System.Net;
using System.Reflection;
using System.Text;


// Warns at startup if Kestrel itself is bound to a non-loopback address with no HTTPS endpoint
// configured. Deliberately does NOT fire for the recommended "reverse proxy on this same
// machine" topology (IIS out-of-process, nginx, Caddy) - there Kestrel keeps listening on
// loopback only (e.g. http://localhost:7098) while the proxy, on 0.0.0.0/a public IP, is the
// one holding the certificate. It also doesn't fire for IIS in-process hosting, which bypasses
// Kestrel's configured endpoints entirely. It only fires for the genuinely risky case: Kestrel
// itself rebound to a LAN/external IP with no TLS anywhere. See DEPLOYMENT.md.
void _warnIfExposedWithoutHttps(IConfiguration configuration, Serilog.ILogger startupLogger)
{
    bool hasHttpsEndpoint = false;
    List<string> nonLoopbackHttpUrls = [];

    foreach (IConfigurationSection endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
    {
        string? url = endpoint["Url"];
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            continue;

        if (uri.Scheme == "https")
        {
            hasHttpsEndpoint = true;
        }
        else if (uri.Scheme == "http" && uri.Host is not ("localhost" or "127.0.0.1" or "::1"))
        {
            nonLoopbackHttpUrls.Add(url);
        }
    }

    if (!hasHttpsEndpoint && nonLoopbackHttpUrls.Count > 0)
    {
        startupLogger.Warning(
            "OSRobot is listening on {Urls} without HTTPS configured. If this is reachable " +
            "beyond a trusted machine/LAN (a real network IP, not localhost), login tokens and " +
            "all traffic travel in plaintext. Put a reverse proxy (IIS/nginx) with TLS in front " +
            "of Kestrel, or configure a direct Kestrel HTTPS endpoint - see DEPLOYMENT.md.",
            nonLoopbackHttpUrls);
    }
}


void _initConfigDatabase(string dbConnectionString)
{
    AssemblyName info = Assembly.GetExecutingAssembly().GetName();
    string name = info.Name!;

    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{name}.Resources.InitDB.txt")!;
    using StreamReader streamReader = new(stream, Encoding.UTF8);
    string initDBScript = streamReader.ReadToEnd();

    using SqliteConnection connection = new(dbConnectionString);
    using SqliteCommand command = new(initDBScript, connection);
    connection.Open();
    command.ExecuteNonQuery();
}

// Idempotent, additive schema upgrade for a database that already existed before the
// MustChangePassword/FailedLoginAttempts/LockedOutUntil columns were introduced. Runs every
// startup, unconditionally - unlike _initConfigDatabase, which only ever runs once against a
// brand new file. New columns default to "not locked out, no forced change" for an existing
// database, since we have no way to know whether its admin password was already changed - only
// a freshly seeded database (InitDB.txt) starts with MustChangePassword forced on.
void _upgradeUserSchema(string dbConnectionString)
{
    using SqliteConnection connection = new(dbConnectionString);
    connection.Open();

    HashSet<string> existingColumns = [];
    using (SqliteCommand pragma = new("PRAGMA table_info(Users);", connection))
    using (SqliteDataReader reader = pragma.ExecuteReader())
    {
        int nameOrdinal = reader.GetOrdinal("name");
        while (reader.Read())
            existingColumns.Add(reader.GetString(nameOrdinal));
    }

    void AddColumnIfMissing(string columnName, string columnDefinition)
    {
        if (existingColumns.Contains(columnName))
            return;
        using SqliteCommand alter = new($"ALTER TABLE Users ADD COLUMN {columnDefinition};", connection);
        alter.ExecuteNonQuery();
    }

    AddColumnIfMissing("MustChangePassword", "MustChangePassword INTEGER NOT NULL DEFAULT 0");
    AddColumnIfMissing("FailedLoginAttempts", "FailedLoginAttempts INTEGER NOT NULL DEFAULT 0");
    AddColumnIfMissing("LockedOutUntil", "LockedOutUntil TEXT NULL");
}

void _initConfigJobsFile(string jobsConfigPathName)
{
    AssemblyName info = Assembly.GetExecutingAssembly().GetName();
    string name = info.Name!;

    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{name}.Resources.InitJobs.txt")!;
    using StreamReader streamReader = new(stream, Encoding.UTF8);
    string initConfigJobsFile = streamReader.ReadToEnd();

    using StreamWriter sw = new(jobsConfigPathName);
    sw.Write(initConfigJobsFile);
}

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
};

var builder = WebApplication.CreateBuilder(options);

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

// configure strongly typed settings object
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Server-side enforcement of "must change password before anything else" - see
    // MustChangePasswordFilter / SECURITY.md.
    options.Filters.Add<MustChangePasswordFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development allow any origin for ease of local frontend development
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // In production restrict to origins listed in AppSettings:AllowedOrigins (comma-separated)
            string[]? allowedOrigins = builder.Configuration["AppSettings:AllowedOrigins"]
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (allowedOrigins != null && allowedOrigins.Length > 0)
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
        }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        // TODO: Validate configuration and remove "!" for JWT:Key 
        ValidIssuer = builder.Configuration["AppSettings:JWT:Issuer"],
        ValidAudience = builder.Configuration["AppSettings:JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:JWT:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();
builder.Logging.AddSerilog(logger);

// Trust X-Forwarded-* headers only from a known reverse proxy, so the app sees the client's
// real scheme/IP when TLS is terminated upstream (IIS out-of-process, nginx, Caddy). Loopback
// (127.0.0.1/::1) is trusted by default - already covers a proxy running on this same machine,
// which is the recommended setup (see DEPLOYMENT.md). AppSettings:ReverseProxy:KnownProxies /
// KnownNetworks only need filling in if the proxy runs on a different host.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    string knownProxies = builder.Configuration["AppSettings:ReverseProxy:KnownProxies"] ?? string.Empty;
    foreach (string ip in knownProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (IPAddress.TryParse(ip, out IPAddress? address))
            options.KnownProxies.Add(address);
    }

    string knownNetworks = builder.Configuration["AppSettings:ReverseProxy:KnownNetworks"] ?? string.Empty;
    foreach (string cidr in knownNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (System.Net.IPNetwork.TryParse(cidr, out System.Net.IPNetwork network))
            options.KnownIPNetworks.Add(network);
    }
});

// Initialize Sqlite configuration database
string dbConfigPathName = Path.Combine(builder.Configuration["AppSettings:JobEngineConfig:DataPath"]!, "config.db");
string dbConnectionString = $"Data Source={dbConfigPathName};";
if (!File.Exists(dbConfigPathName))
{
    _initConfigDatabase(dbConnectionString);
}
_upgradeUserSchema(dbConnectionString);

// Initialize jobs configuration file
string jobsConfigPathName = Path.Combine(builder.Configuration["AppSettings:JobEngineConfig:DataPath"]!, "jobs.json");
if (!File.Exists(jobsConfigPathName))
{
    _initConfigJobsFile(jobsConfigPathName);
}

// JobEngine registration. The engine itself is a plain DI singleton; its lifecycle
// (Start/Stop) is driven by JobEngineHostedService below, which the generic host
// invokes only once all services have finished being built, and calls back into on
// shutdown.
builder.Services.AddSingleton<Serilog.ILogger>(logger);
builder.Services.AddSingleton<IAppLogger, AppLogger>();

// Dedicated audit trail - logins, lockouts, job-configuration saves, manual task starts - kept
// separate from the general application log (its own rolling file) so it's easy to review or
// ship to a SIEM independently of routine framework/app noise. See SECURITY.md.
string auditLogPath = Path.Combine(builder.Configuration["AppSettings:JobEngineConfig:LogPath"]!, "audit-.log");
Serilog.ILogger auditLogger = new LoggerConfiguration()
    .WriteTo.File(auditLogPath, rollingInterval: RollingInterval.Day,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Message:lj}{NewLine}")
    .CreateLogger();
builder.Services.AddSingleton<IAuditLogger>(new AppLogger(auditLogger));

builder.Services.AddSingleton<IJobEngineConfig>(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value.JobEngineConfig);
builder.Services.AddSingleton<IJobEngine, JobEngine>();
builder.Services.AddHostedService<JobEngineHostedService>();

builder.Services.AddDbContext<RobotDBContext>(options => options.UseSqlite(dbConnectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJWTManager, JWTManager>();

builder.Host.UseWindowsService();

var app = builder.Build();

// Must run before anything that inspects Request.Scheme or the remote IP - notably
// UseHttpsRedirection and authentication - so a reverse-proxied HTTPS request is recognized
// as HTTPS rather than as the plain HTTP connection the proxy actually makes to Kestrel.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefPolicy");

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(options.ContentRootPath!, "Public")),
    RequestPath = "/Public"
});

app.UseHttpsRedirection();

// Explicit rather than relying on WebApplication's automatic middleware insertion - keeps the
// pipeline order visible and correct regardless of future framework behavior changes.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

_warnIfExposedWithoutHttps(builder.Configuration, logger);

app.Run();
