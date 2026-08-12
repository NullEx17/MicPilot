# Security Policy

## Supported versions

MicPilot is in early development. Security fixes apply to the latest `main` branch.

## Reporting a vulnerability

Please report security issues privately rather than opening a public GitHub issue.

Include:

- A description of the issue
- Steps to reproduce
- Potential impact
- Your MicPilot version and Windows version

## Scope

MicPilot is designed to:

- Process audio locally only
- Not record or upload microphone audio
- Not download or execute binaries from the internet
- Not require administrator privileges for normal operation

Driver installation (VB-CABLE) is a separate user action performed outside MicPilot.

## Out of scope

- Issues in third-party drivers (VB-CABLE, etc.)
- Game anti-cheat interactions with virtual audio devices
- Discord or game-specific behavior outside MicPilot's routing model
