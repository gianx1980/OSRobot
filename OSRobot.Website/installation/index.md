---
title: "Installation"
description: "How to install OS-Robot on Windows, on Linux and in a Docker container, and how to reach the Job Editor for the first time."
---

# Installation

This page describes the steps required to install OS-Robot on your machine.

A prerequisite for the installation is the presence of the ASP.NET 10 runtime, which can be downloaded from Microsoft [at this address](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

### Windows installation

Download the ZIP file containing the application binaries from [this page](/downloads/).

Extract the ZIP file into a folder, for example, “C:\\Program Files\\os-robot”.

Create the necessary folders to store logs and data, open the file “bin\\appsettings.json” and configure the following keys:

-   AppSettings/JobEnginConfig/LogPath
-   AppSettings/JobEnginConfig/DataPath
-   SeriLog/WriteTo/Args/path

```text
{
  "AppSettings": {
    "JobEngineConfig": {
      "LogPath": "C:\\Program Files\\os-robot\\logs",
      "DataPath": "C:\\Program Files\\os-robot\\data",
  ....
  ....
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:\\Program Files\\os-robot\\logs\\os-robot-.log",
          ...
          ...
        }
      }
    ]
  }
```

Open a command prompt as an administrator and run the following command to make the OS-Robot JobEngine executable as a service:

```text
sc create "OS-Robot JobEngine" binPath= "C:\Program Files\os-robot\bin\OSRobot.Server.exe"
```

Start the service:

```text
sc start "OS-Robot JobEngine"
```

### Linux installation

Download the ZIP file containing the application binaries from [this page](/downloads/).

Extract the ZIP file into a folder, for example, “/opt/os-robot-1.0.0”.

Create the necessary folders to store logs and data, open the file “bin/appsettings.json” and configure the following keys:

-   AppSettings/JobEnginConfig/LogPath
-   AppSettings/JobEnginConfig/DataPath
-   SeriLog/WriteTo/Args/path

```text
{
  "AppSettings": {
    "JobEngineConfig": {
      "LogPath": "/opt/os-robot/logs",
      "DataPath": "/opt/os-robot/data",
  ....
  ....
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/opt/os-robot/logs/os-robot-.log",
          ...
          ...
        }
      }
    ]
}
```

Create the file /etc/systemd/os-robot.service with the following content:

```text
[Unit]
Description=OS-Robot JobEngine
After=network.target

[Service]
WorkingDirectory=/opt/osrobot/bin
ExecStart=/usr/bin/dotnet /opt/os-robot/bin/OSRobot.Server.dll
Restart=always
User=root
Group=root
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

From the command line, execute the following commands:

```text
# Reload systemd to recognize the new service
sudo systemctl daemon-reload

# Enable the service to start on boot
sudo systemctl enable /etc/systemd/osrobot.service

# Start the service
sudo systemctl start osrobot
```

### Docker installation

As a prerequisite, Docker must be installed on the machine where you want to build the image and, of course, in the machine where you want to execute the container. In the instructions below we will assume a Windows machine.

Download the ZIP file containing the application binaries from [this page](/downloads/).

Extract the ZIP file into a folder, for example, “D:\\TempContainer\\os-robot”.

To build the image, you can use the following “dockerfile” that you have to place in the folder “D:\\TempContainer”.

```text
# Use official .NET 10 ASP.NET runtime image as base
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 5000

# Copy published files
COPY ./os-robot/ ./

# Start the application
ENTRYPOINT ["dotnet", "./bin/OSRobot.Server.dll"]
```

Modify the file “bin\\appsettings.json” to use the “/app” folder specified in the dockerfile.

```text
{
  "AppSettings": {
    "JobEngineConfig": {
      "LogPath": "/app/logs",
      "DataPath": "/app/data",
  ....
  ....
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/app/logs/os-robot-.log",
          ...
          ...
        }
      }
    ]
  }
```

Open the file “bin\\Public\\AppConfig.js” and modify the http port to 32700 (this is the port we will use to expose os-robot’s api, if you want you can use a different one):

```text
var AppConfig = {
  appTitle: "OSRobot",
  apiUrl: "http://localhost:32700/api",
};
```

We need a folder to in the host operating system to save OS-Robot’s configurations files. In the example we will assume the folder “D:\\ContainerData”.

We are now ready to build the container image. Open the command line, go to the folder “D:\\TempContainer” and execute the following commands:

```text
docker build -t osrobot-image -f dockerfile .

docker run -d -p 32700:5000 -v D:\ContainerData:/app/Data osrobot-image
```

### Security concerns

The installation described above, for running the OS-Robot service, assumes the use of a user with elevated privileges: “LocalSystem” for Windows and “root” for Linux.

This may be acceptable for a development or testing environment but should be carefully evaluated in a production environment.

If possibile, avoid using administrative accounts in a production environment!  

### Final considerations

For both operating systems, Windows or Linux, you can access the OS-Robot user interface using any browser by navigating to the default address `http://localhost:5000/Public/`.

If you are running OS-Robot in a container the default address is the following: `http://localhost:32700/Public/`.

The default credentials to access the Job Editor are:

-   User: admin
-   Password: admin
