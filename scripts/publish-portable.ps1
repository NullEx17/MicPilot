# Builds a zip normal people can download from GitHub.
# Unzip and run MicPilot.exe — no Visual Studio, no `dotnet` needed.
# Still requires VB-CABLE.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Get-Process MicPilot -ErrorAction SilentlyContinue | Stop-Process -Force

$version = "1.0.0"
$out = Join-Path $root "artifacts\MicPilot-$version-win-x64"
$zip = Join-Path $root "artifacts\MicPilot-$version-win-x64.zip"

if (Test-Path $out) {
    Remove-Item $out -Recurse -Force
}
if (Test-Path $zip) {
    Remove-Item $zip -Force
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

dotnet publish src\MicPilot.App\MicPilot.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $out

@"
MicPilot $version
No more Push-to-Talk.

1. Install VB-CABLE first: https://vb-audio.com/Cable/
2. Double-click MicPilot.exe
3. You do not need Visual Studio or the .NET SDK

Discord stays on your real mic. Games use CABLE Output.
Hotkey mutes the game mic only.

by NullEx17
https://nullex17.me
"@ | Set-Content -Path (Join-Path $out "START HERE.txt") -Encoding UTF8

Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -Force

Write-Host ""
Write-Host "Release zip:"
Write-Host "  $zip"
Write-Host ""
Write-Host "Upload that file to GitHub Releases."
