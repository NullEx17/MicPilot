# Contributing to MicPilot

Thanks for your interest in contributing.

## Getting started

1. Fork the repository.
2. Install .NET 8 SDK.
3. Install VB-CABLE if you plan to work on audio features.
4. Build and run:

```powershell
dotnet build
dotnet test
dotnet run --project src/MicPilot.App
```

## Guidelines

- Keep changes focused and small.
- Do not claim features work until they are tested.
- MicPilot must remain local-first: no telemetry, no cloud accounts, no microphone recording.
- Prefer clear user-facing error messages over raw exceptions.
- Match the existing code style: simple, readable, no unnecessary abstractions.

## Pull requests

- Describe what changed and why.
- Note any limitations or follow-up work.
- Ensure `dotnet build` and `dotnet test` pass.

## Reporting issues

Use the GitHub issue templates. Include diagnostics from **Settings → Copy Diagnostics** when reporting audio problems.

Do not attach voice recordings or private audio files.
