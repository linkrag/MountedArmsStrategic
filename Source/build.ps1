# Builds 1.6/Assemblies/MountedArmsStrategic.dll with the .NET Framework compiler (C# 5; no SDK/mono needed).
# usage: powershell -File Source/build.ps1 [-RW <RimWorld folder>] [-CE <Combat Extended folder>]
param(
    [string]$RW = "F:\Steam\steamapps\common\RimWorld",
    [string]$CE = "F:\Steam\steamapps\workshop\content\294100\2890901044"
)
$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$m = "$RW\RimWorldWin64_Data\Managed"
$root = Split-Path $PSScriptRoot -Parent
$out = "$root\1.6\Assemblies"
New-Item -ItemType Directory -Force $out | Out-Null
# /noconfig /nostdlib: compile against the game's own mscorlib/System, not the Windows ones
& $csc /nologo /noconfig /target:library /optimize+ /debug- /nostdlib /out:"$out\MountedArmsStrategic.dll" `
    /r:"$m\mscorlib.dll" /r:"$m\System.dll" /r:"$m\System.Core.dll" /r:"$m\Assembly-CSharp.dll" /r:"$m\UnityEngine.dll" /r:"$m\UnityEngine.CoreModule.dll" /r:"$m\netstandard.dll" `
    /r:"$CE\Assemblies\CombatExtended.dll" `
    "$PSScriptRoot\*.cs"
if ($LASTEXITCODE -ne 0) { throw "csc failed ($LASTEXITCODE)" }
Get-FileHash "$out\MountedArmsStrategic.dll" -Algorithm MD5 | Select-Object Hash, Path
