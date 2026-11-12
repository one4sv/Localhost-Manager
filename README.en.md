# Localhost Manager

[Русский](README.ru.md) | [English](README.en.md)

Localhost Manager is a Windows desktop application for managing local development projects and localhost services.

## Features

- Projects with multiple localhost services
- Start, stop and restart localhost services
- Custom startup commands
- System tray integration
- Favorite localhosts
- Start, stop and critical error notifications
- Dark and light themes
- Russian and English languages
- Start with Windows
- Port and URL management
- Process logs
- Persistent project and application settings

## Download

The latest Windows x64 build is available in [Releases](../../releases).

Download:

`LocalhostManager-v0.2-win-x64.zip`

Extract the archive to a convenient folder and run `LocalhostManager.exe`.

The application is published as self-contained, so .NET 8 Runtime does not need to be installed separately.

## Running from source

Requirements:

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or another compatible editor

Clone the repository:

```bash
git clone https://github.com/one4sv/Localhost-Manager.git
cd Localhost-Manager
```

Run the application:

```bash
dotnet run --project LocalhostManager/LocalhostManager.csproj
```

## Build

For a Release build:

```bash
dotnet build LocalhostManager/LocalhostManager.csproj -c Release
```

To create a self-contained Windows x64 build:

```bash
dotnet publish LocalhostManager/LocalhostManager.csproj -c Release -r win-x64 --self-contained true
```

The published files will be available in:

```text
LocalhostManager/bin/Release/net8.0-windows/win-x64/publish/
```

## Usage

### Create a project

1. Click `Add project`.
2. Enter a project name and description.
3. Save the project.

### Add a localhost service

1. Open the required project.
2. Click `Add localhost`.
3. Enter a name.
4. Select or add a startup command.
5. Select the project folder.
6. Optionally specify a port and URL.
7. Save the localhost service.

The service can then be started, stopped and restarted directly from the application.

## System tray

The system tray provides quick access to:

- projects;
- localhost services;
- favorite localhosts;
- start all projects;
- stop all projects;
- restart all projects;
- opening the application;
- settings;
- exit.

Up to three localhosts can be added to favorites.

## Settings

Available settings include:

- start the application with Windows;
- minimize to tray on startup;
- send the window to the tray when closed;
- dark and light themes;
- Russian and English languages;
- start notifications;
- stop notifications;
- critical error notifications;
- favorite localhosts;
- startup commands.

## Logs

Logs can be opened for running localhost services to view process output.

## Data storage

Projects and application settings are stored locally on the user's computer.

They are not stored in the GitHub repository and are not included in the published application ZIP.

## Tech stack

- C#
- .NET 8
- WPF
- XAML
- Windows Forms

## Version

Current public version:

`v0.2`

This is an early public release. The functionality and interface are still evolving.

## License

MIT

## Changes after v0.1

Settings → Project startup lets you enable automatic startup and choose projects. It is
**off by default** and applies once on the next app launch, including Windows sign-in.
Returning from the tray does not restart projects. Windows startup controls the app only.
Persisted runtime status is ignored. Automatic startup is skipped after data recovery.

The home screen adds project search and service counts. Restart events cannot clear a
replacement process; stop errors are reported. Logs retain the latest 2000 lines and
update in batches. JSON writes are atomic, with `.bak` backups and `.corrupt-<timestamp>`
copies preserved on recovery.

Run regression checks on Windows:

```powershell
dotnet run --project LocalhostManager.Tests/LocalhostManager.Tests.csproj -c Release
```

Tests use isolated temporary data and test processes. UI previews are written to
`artifacts/screenshots`. Set `LOCALHOST_MANAGER_DATA_DIR` to override the data directory
for isolated runs; otherwise the app uses `%AppData%/LocalhostManager`.

## Running and diagnosing services

Select a project in the sidebar to see its services and recent output. Open Logs to filter messages, copy selected lines, export the filtered view or pause automatic scrolling. Standard error is marked separately.

Choose a service folder to discover package.json scripts, .NET projects and common Python entry points. Review the suggested command before saving. In advanced settings, choose the startup order (lower numbers first), timeout and an optional HTTP health URL. A port alone enables TCP readiness checks. Without a probe the app reports process state. Project startup waits for each configured probe before starting the next service. Port diagnostics identify the listening process and PID.

A service can provide its own graceful shutdown command. Otherwise the manager closes stdin and requests window closure. After a three-second grace period it terminates the process tree. Optional crash recovery makes up to three attempts after 2, 4 and 6 seconds. Crash history is available in Settings. Launching the manager a second time activates its existing window.

Settings can export project configurations, import additional projects and restore the previous backup. Imported projects get fresh service IDs and automatic startup is disabled. Restoring replaces the list and preserves the original IDs.
