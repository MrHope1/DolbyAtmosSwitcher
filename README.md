# Dolby Atmos Switcher

Dolby Atmos Switcher is a small, unofficial Windows tray utility for switching between the **Game**, **Movie**, **Music**, and **Voice** profiles in the Dolby Access app.

It supports both a tray menu and a command-line interface. The utility performs local Windows UI Automation only; it does not include networking, analytics, or telemetry.

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

## Run

Start the tray application:

```powershell
.\DolbyAtmosSwitcher.exe
```

Switch a profile from the command line:

```powershell
.\DolbyAtmosSwitcher.exe /change Game
.\DolbyAtmosSwitcher.exe /change Movie
.\DolbyAtmosSwitcher.exe /change Music
.\DolbyAtmosSwitcher.exe /change Voice
```

CLI exit codes are `0` for success, `1` when profile switching fails, and `2` for invalid usage.

The tray menu also includes an optional **Run at Startup** toggle. It writes only to the current user's Windows Run key.

## Install

Build the executable first, then open an Administrator PowerShell window in the repository directory and run:

```powershell
.\install.ps1
```

The installer copies only the locally built executable to `C:\Program Files\DolbySwicher`. Source files and runtime logs are not copied.

## Privacy and security

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

## Notes

The utility drives the English-language Dolby Access interface and expects the current profile names used by that app. UI changes in future Dolby Access versions may require selector updates.

This project is not affiliated with or endorsed by Dolby Laboratories. Dolby and Dolby Atmos are trademarks of Dolby Laboratories.

## License

No license has been granted yet. All rights are reserved unless a license is added later.
