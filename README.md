# Dolby Atmos Profile Switcher for Windows

![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)
![C%23](https://img.shields.io/badge/C%23-.NET%20Framework-512BD4?logo=dotnet)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![GitHub stars](https://img.shields.io/github/stars/MrHope1/DolbyAtmosSwitcher?style=flat)

**Dolby Atmos Switcher** is a lightweight, unofficial **Dolby Access / Dolby Atmos profile switcher for Windows 10 and Windows 11**. Switch between **Game, Movie, Music, and Voice** from the Windows system tray or command line without repeatedly navigating the Dolby Access app.

The utility works locally through Windows UI Automation. It does not include networking, analytics, or telemetry.

> This project is not affiliated with or endorsed by Dolby Laboratories.

## Why use it?

Dolby Access makes you open the app and navigate its interface whenever you want to change profiles. Dolby Atmos Switcher exposes those profile changes through a small tray utility and CLI instead.

Useful for people who regularly switch between gaming, movies, music, and voice-focused audio setups on Windows.

## Features

- Switch Dolby Access profiles from the **Windows system tray**
- Switch profiles from the **command line (CLI)**
- Supports **Game**, **Movie**, **Music**, and **Voice**
- Optional **Run at Startup** toggle for the current Windows user
- Uses local **Windows UI Automation**
- No network requests, analytics, or telemetry
- Supports **Windows 10 and Windows 11**

## Quick start

### Requirements

- Windows 10 or Windows 11
- Dolby Access installed from the Microsoft Store
- A .NET Framework 4.x C# compiler available on the system

### Build from source

Clone the repository and run the build script in PowerShell:

```powershell
git clone https://github.com/MrHope1/DolbyAtmosSwitcher.git
cd DolbyAtmosSwitcher
.\build.ps1
```

The build creates `DolbyAtmosSwitcher.exe` in the repository directory.

Start the tray app:

```powershell
.\DolbyAtmosSwitcher.exe
```

## Downloads and releases

A packaged binary release has not been published yet. For now, build the executable from source using the steps above.

Future packaged versions will appear on the [GitHub Releases page](https://github.com/MrHope1/DolbyAtmosSwitcher/releases).

## Switch profiles from the command line

```powershell
.\DolbyAtmosSwitcher.exe /change Game
.\DolbyAtmosSwitcher.exe /change Movie
.\DolbyAtmosSwitcher.exe /change Music
.\DolbyAtmosSwitcher.exe /change Voice
```

CLI exit codes:

- `0` — profile switched successfully
- `1` — profile switching failed
- `2` — invalid usage

## Install

Build the executable first, then open an Administrator PowerShell window in the repository directory and run:

```powershell
.\install.ps1
```

The installer copies only the locally built executable to `C:\Program Files\DolbySwicher`. Source files and runtime logs are not copied.

## Run at Windows startup

The tray menu includes an optional **Run at Startup** toggle. It writes only to the current user's Windows Run key.

## How it works

Dolby Atmos Switcher controls the English-language Dolby Access interface with Windows UI Automation. It does not replace Dolby Access, modify Dolby's audio drivers, or use a remote service.

Because the project depends on the Dolby Access user interface, future UI changes may require selector updates.

## Privacy and security

- No network requests, telemetry, or analytics
- No credentials, account data, or personal information stored by the project
- Runtime diagnostics are stored locally under `%LOCALAPPDATA%\DolbyAtmosSwitcher`
- Runtime logs and compiled binaries are excluded from Git
- Build and install scripts resolve inputs relative to their own package directory

## Tests

Run the complete local test suite:

```powershell
.\tests\Run-Tests.ps1
```

The suite covers profile validation, Windows system executable resolution, per-user log placement, CLI exit codes, and build/install path isolation.

## Compatibility

Dolby Atmos Switcher currently targets the English-language Dolby Access interface and these profile names:

- Game
- Movie
- Music
- Voice

If Dolby changes the relevant interface elements or labels, the automation may need to be updated.

## Troubleshooting

### The profile does not switch

Make sure Dolby Access is installed, starts correctly, and uses the English profile names listed above. If Dolby Access has changed its interface, please open a bug report with your Windows version and Dolby Access version.

### Where are the logs?

Runtime diagnostics are stored under:

```text
%LOCALAPPDATA%\DolbyAtmosSwitcher
```

### Can I use it without building from source?

Not yet. There is currently no packaged GitHub Release. A future release can provide a prebuilt executable so normal users do not need the compiler toolchain.

## Contributing

Bug reports, compatibility reports, and focused pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a change.

Good contributions include:

- Fixes for Dolby Access UI changes
- Better compatibility across Windows versions
- More resilient UI Automation selectors
- Documentation and troubleshooting improvements

## Support the project

If Dolby Atmos Switcher saves you time, the easiest ways to support it today are to **star the repository**, share it with other Dolby Access users, report compatibility issues, and contribute fixes.

The repository also includes GitHub funding configuration so a Sponsor button can be enabled as soon as a funding account is connected.

## FAQ

### Does this switch Dolby Atmos profiles on Windows 11?

Yes. Windows 11 is supported, provided Dolby Access is installed and its interface matches the profile names expected by the utility.

### Can I switch Dolby Access profiles without opening the app manually every time?

Yes. The utility automates the Dolby Access interface and exposes profile switching through its tray menu and CLI.

### Which Dolby Atmos profiles can it switch between?

The currently supported profile names are **Game**, **Movie**, **Music**, and **Voice**.

### Does Dolby Atmos Switcher send telemetry or make network requests?

No. Profile switching and diagnostics are local to the Windows machine.

### Is this an official Dolby application?

No. This is an independent, unofficial open-source utility and is not affiliated with or endorsed by Dolby Laboratories.

## License

Released under the [MIT License](LICENSE).

## Disclaimer

Dolby and Dolby Atmos are trademarks of Dolby Laboratories. This project is not affiliated with or endorsed by Dolby Laboratories.
