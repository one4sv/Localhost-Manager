# Localhost Manager

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

`LocalhostManager-v0.1-win-x64.zip`

Extract the archive to a convenient folder and run `LocalhostManager.exe`.

The application is published as self-contained, so .NET 8 Runtime does not need to be installed separately.

## Running from source

Requirements:

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or another compatible editor

Clone the repository:

```bash
git clone https://github.com/one4sv/LocalhostManager.git
cd LocalhostManager
```

Run the application:

```bash
dotnet run
```

## Build

For a Release build:

```bash
dotnet build -c Release
```

To create a self-contained Windows x64 build:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
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

`v0.1`

This is an early public release. The functionality and interface are still evolving.

## License

MIT
