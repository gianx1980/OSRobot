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
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OSRobot.Server;
using OSRobot.Server.Configuration;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Infrastructure.Security;
using OSRobot.Server.Infrastructure.Security.Abstract;
using OSRobot.Server.JobEngineLib;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using Serilog;
using System.Reflection;
using System.Text;


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
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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

// Initialize Sqlite configuration database
string dbConfigPathName = Path.Combine(builder.Configuration["AppSettings:JobEngineConfig:DataPath"]!, "config.db");
string dbConnectionString = $"Data Source={dbConfigPathName};";
if (!File.Exists(dbConfigPathName))
{
    _initConfigDatabase(dbConnectionString);
}

// Initialize jobs configuration file
string jobsConfigPathName = Path.Combine(builder.Configuration["AppSettings:JobEngineConfig:DataPath"]!, "jobs.json");
if (!File.Exists(jobsConfigPathName))
{
    _initConfigJobsFile(jobsConfigPathName);
}

//// Initialize JobEngine
AppLogger appLogger = new(logger);
JobEngineConfig jobEngineConfig = new()
{
    LogPath = builder.Configuration["AppSettings:JobEngineConfig:LogPath"]!,
    DataPath = builder.Configuration["AppSettings:JobEngineConfig:DataPath"]!,
    SerialExecution = Convert.ToBoolean(builder.Configuration["AppSettings:JobEngineConfig:SerialExecution"]!),
    CleanUpLogsOlderThanHours = Convert.ToInt32(builder.Configuration["AppSettings:JobEngineConfig:CleanUpLogsOlderThanHours"]!),
    CleanUpLogsIntervalHours = Convert.ToInt32(builder.Configuration["AppSettings:JobEngineConfig:CleanUpLogsIntervalHours"]!)
};
JobEngine jobEngine = new(appLogger, jobEngineConfig);
jobEngine.Start();

builder.Services.AddSingleton<IJobEngine>(jobEngine);
builder.Services.AddDbContext<RobotDBContext>(options => options.UseSqlite(dbConnectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJWTManager, JWTManager>();

builder.Host.UseWindowsService();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
