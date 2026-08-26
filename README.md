# Dolby Atmos Profile Switcher for Windows

**Dolby Atmos Switcher** is a small, unofficial **Dolby Access profile switcher for Windows 10 and Windows 11**. It lets you switch between the **Game**, **Movie**, **Music**, and **Voice** profiles in the Dolby Access app from a Windows system tray menu or from the command line.

If you are looking for a lightweight way to change Dolby Atmos / Dolby Access sound profiles without repeatedly navigating the Dolby Access interface, this utility provides a local Windows UI Automation-based workflow. It does not include networking, analytics, or telemetry.

> This project is not affiliated with or endorsed by Dolby Laboratories.

## Features

- Switch Dolby Access profiles from the **Windows system tray**
- Switch profiles with a **command-line interface (CLI)**
- Supports the Dolby Access **Game**, **Movie**, **Music**, and **Voice** profiles
- Optional **Run at Startup** toggle for the current Windows user
- Uses local **Windows UI Automation** to control Dolby Access
- No network requests, analytics, or telemetry
- Works on **Windows 10 and Windows 11**

## Supported Dolby Access profiles

Dolby Atmos Switcher currently targets these English-language Dolby Access profile names:

- Game
- Movie
- Music
- Voice

## Requirements

- Windows 10 or Windows 11
- Dolby Access installed from the Microsoft Store
- The classic .NET Framework 4.x C# compiler included with Windows

## Build

Open PowerShell in the repository directory and run:

```powershell
.\build.ps1
```

The script compiles `Program.cs` and creates `DolbyAtmosSwitcher.exe` beside the build script.

## Run the tray app

Start Dolby Atmos Switcher normally:

```powershell
.\DolbyAtmosSwitcher.exe
```

The application runs as a Windows tray utility and provides profile-switching commands from its tray menu.

## Switch Dolby Atmos profiles from the command line

You can also use Dolby Atmos Switcher as a CLI profile switcher:

```powershell
.\DolbyAtmosSwitcher.exe /change Game
.\DolbyAtmosSwitcher.exe /change Movie
.\DolbyAtmosSwitcher.exe /change Music
.\DolbyAtmosSwitcher.exe /change Voice
```

CLI exit codes are:

- `0` — profile switched successfully
- `1` — profile switching failed
- `2` — invalid usage

## Run at Windows startup

The tray menu includes an optional **Run at Startup** toggle. It writes only to the current user's Windows Run key.

## Install

Build the executable first, then open an Administrator PowerShell window in the repository directory and run:

```powershell
.\install.ps1
```

The installer copies only the locally built executable to `C:\Program Files\DolbySwicher`. Source files and runtime logs are not copied.

## Privacy and security

Dolby Atmos Switcher is designed to operate locally on Windows:

- No network requests, telemetry, or analytics
- No credentials, account data, or personal information stored in the repository
- Runtime diagnostics are stored locally under `%LOCALAPPDATA%\DolbyAtmosSwitcher`
- Runtime logs and compiled binaries are excluded from Git
- Build and install scripts resolve inputs relative to their own package directory

## Tests

Run the complete local test suite:

```powershell
.\tests\Run-Tests.ps1
```

The suite verifies profile validation, Windows system executable resolution, per-user log placement, and build/install path isolation.

## Compatibility notes

The utility drives the **English-language Dolby Access interface** and expects the current profile names used by that app. UI changes in future Dolby Access versions may require selector updates.

## FAQ

### Does this switch Dolby Atmos profiles on Windows 11?

Yes. Windows 11 is supported, provided Dolby Access is installed and its interface matches the profile names expected by the utility.

### Can I switch Dolby Access profiles without opening the app manually each time?

The utility automates the Dolby Access interface and exposes profile switching through its tray menu and CLI, so you do not need to navigate through the app manually for each switch.

### Which Dolby Atmos profiles can it switch between?

The current supported profile names are **Game**, **Movie**, **Music**, and **Voice**.

### Does Dolby Atmos Switcher send telemetry or make network requests?

No. The utility performs local Windows UI Automation and does not include networking, analytics, or telemetry.

## Disclaimer

This project is not affiliated with or endorsed by Dolby Laboratories. Dolby and Dolby Atmos are trademarks of Dolby Laboratories.

## License

No license has been granted yet. All rights are reserved unless a license is added later.
