# Contributing to Dolby Atmos Switcher

Thanks for helping improve Dolby Atmos Switcher.

## Before opening an issue

- Confirm that Dolby Access is installed and launches normally.
- Check whether the problem still occurs after restarting Dolby Access and the switcher.
- Search existing issues to avoid duplicates.
- Do not include passwords, account details, or other private information in logs or screenshots.

## Bug reports

A useful bug report should include:

- Windows version
- Dolby Access version, if available
- Which profile you tried to switch to
- Whether you used the tray menu or CLI
- What you expected to happen
- What actually happened
- Relevant error text or sanitized diagnostic information from `%LOCALAPPDATA%\DolbyAtmosSwitcher`

## Pull requests

Keep changes focused and easy to review. For behavior changes or fixes:

1. Add or update a test when practical.
2. Run the full test suite:

```powershell
.\tests\Run-Tests.ps1
```

3. Build the application:

```powershell
.\build.ps1
```

4. Explain what changed and why in the pull request description.

## Scope

Contributions that fit the project especially well include:

- Dolby Access UI compatibility fixes
- More resilient UI Automation behavior
- Windows compatibility improvements
- CLI reliability improvements
- Documentation and troubleshooting improvements

Large unrelated refactors are harder to review and may be declined even if technically valid.

## Project status

This is an unofficial community project. Dolby Access UI changes can break automation, so compatibility reports from users are valuable.
