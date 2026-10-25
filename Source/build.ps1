# Builds MountedArmsStrategic.dll with the .NET Framework compiler (C# 5; no SDK/mono needed).
# The game folder decides the target:
#   RimWorld 1.6 (Unity/mono, RimWorldWin64_Data\Managed)  -> Bin/1.6/Assemblies
#   RimWorld 1.7 (Waylith/.NET 10, DLLs at the game root)  -> Bin/1.7/Assemblies (references UnityStubs: 1.7 only)
# On 1.7 the 1.6 CE is compiled against through the facades from tools/unity_facades.ps1.
# usage: powershell -File Source/build.ps1 [-RW <RimWorld folder>] [-CE <Combat Extended folder>]
# -Out: alternative output folder (e.g. to check the build while the game has the DLL open)
param(
    [string]$RW = "F:\Steam\steamapps\common\RimWorld",
    [string]$CE = "F:\Steam\steamapps\workshop\content\294100\2890901044",
    [string]$HarmonyMod = "F:\Steam\steamapps\workshop\content\294100\2009463077",
    [string]$Out = ""
)
$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$root = Split-Path $PSScriptRoot -Parent
$m = "$RW\RimWorldWin64_Data\Managed"
if (Test-Path $m) {
    $version = "1.6"
    # /noconfig /nostdlib: compile against the game's own mscorlib/System, not the Windows ones
    $refs = @("mscorlib", "System", "System.Core", "Assembly-CSharp", "UnityEngine", "UnityEngine.CoreModule", "netstandard", "Unity.Mathematics") |
        ForEach-Object { "/r:$m\$_.dll" }
} else {
    $version = "1.7"
    $facades = "$root\obj\facades"
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$root\tools\unity_facades.ps1" -RW $RW -Out $facades
    if ($LASTEXITCODE -ne 0) { throw "facades failed ($LASTEXITCODE)" }
    $refs = @("mscorlib", "netstandard", "System.Runtime", "System.Private.CoreLib", "System.Collections", "System", "System.Core", "System.Linq", "Assembly-CSharp", "UnityStubs", "Waylith") |
        ForEach-Object { "/r:$RW\$_.dll" }
    $refs += Get-ChildItem $facades -Filter *.dll | ForEach-Object { "/r:" + $_.FullName }
}
$out = if ($Out) { $Out } else { "$root\Bin\$version\Assemblies" }
New-Item -ItemType Directory -Force $out | Out-Null
& $csc /nologo /noconfig /target:library /optimize+ /debug- /nostdlib /nowarn:1701,1684 /out:"$out\MountedArmsStrategic.dll" `
    @refs /r:"$CE\Assemblies\CombatExtended.dll" /r:"$HarmonyMod\$version\Assemblies\0Harmony.dll" `
    "$PSScriptRoot\*.cs"
if ($LASTEXITCODE -ne 0) { throw "csc failed ($LASTEXITCODE)" }
"RimWorld $version"
Get-FileHash "$out\MountedArmsStrategic.dll" -Algorithm MD5 | Select-Object Hash, Path
