# MicPilot

**No more Push-to-Talk.**

Windows app by [NullEx17](https://nullex17.me).

I made this because of FiveM roleplay. In RP servers you use push-to-talk in-game so people nearby don't hear you talking out of character. The annoying part is Discord — if the game is using the same mic, the second you talk to friends in Discord, people in the city hear it too. That's how you end up HRP'ing by accident.

MicPilot splits that. Discord keeps your real microphone. The game gets a virtual one you can mute with a hotkey. Talk in Discord as much as you want. Nobody in-game hears it unless you want them to.

## How it works

| App | Microphone |
|---|---|
| Discord | Your real mic |
| Game / FiveM / whatever | `CABLE Output (VB-Audio Virtual Cable)` |

Hotkey (default **PgDn**) mutes only the game route. Discord is untouched.

## Download

Grab the latest zip from [Releases](https://github.com/NullEx17/MicPilot/releases). Unzip it and run `MicPilot.exe`. You do **not** need Visual Studio.

You will need:

1. [VB-CABLE](https://vb-audio.com/Cable/) (install this yourself)
2. Windows 10/11 64-bit

That's it. The release already includes .NET.

## Setup

1. Install VB-CABLE
2. Run MicPilot
3. Pick your physical microphone
4. In the game, set input to **CABLE Output (VB-Audio Virtual Cable)**
5. Leave Discord on your real mic
6. Add the game under **Games & Apps** (FiveM, VALORANT, etc.)
7. Use your hotkey

Green = game mic ON. Red = MUTED.

## Features

- Routes your mic into a virtual cable for games
- Global hotkey (toggle or hold-to-talk)
- Per-game profiles
- Mute overlay
- Tray icon (ON / MUTED)
- Recovers if Windows rearranges audio devices
- Starts with Windows if you want
- Everything stays on your PC. No accounts, no cloud, no recording.

## Building from source

Only if you want to compile it yourself:

```powershell
dotnet build
dotnet test
dotnet run --project src/MicPilot.App
```

Release zip for GitHub:

```powershell
.\scripts\publish-portable.ps1
```

Output: `artifacts\MicPilot-1.0.0-win-x64.zip`

## Overlay

The overlay is click-through. It does not inject into games. Windowed / borderless works best. Exclusive fullscreen can hide it — that's Windows, not MicPilot.

## Honest limits

- If a game is still using your physical mic, MicPilot cannot mute that game
- One virtual cable = one game route
- Discord voice and stream mic are separate Discord settings, not MicPilot
- "Running" just means the process is open, not that the game picked the virtual mic
- Some FiveM servers only follow the Windows default recording device

## Credits

MicPilot — No more Push-to-Talk.

NullEx17  
https://nullex17.me  
Discord: @NullEx17  
GitHub: https://github.com/NullEx17

MIT license. See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
