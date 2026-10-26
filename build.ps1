# Builds MountedArmsStrategic.dll with the .NET SDK (10+) for both game versions:
#   RimWorld 1.6 (net472)  -> Bin/1.6/Assemblies
#   RimWorld 1.7 (net10.0) -> Bin/1.7/Assemblies
# Close the game first (it locks the DLL). -Out <folder>: build there instead of Bin/ (just to check the compile).
# 1.7 needs the 1.7 game folder (-RW) for the UnityEngine.* -> UnityStubs facades (CE is still the 1.6 build).
# -CE17 <CombatExtended.dll>: once a CE 1.7 exists, build 1.7 against it (no facades; 1.6 keeps the workshop CE).
# -Workshop <folder>: Steam workshop content folder of RimWorld (294100), for Harmony and CE.
# usage: powershell -File build.ps1 [-Version all|1.6|1.7] [-Out <folder>] [-RW <RimWorld 1.7 folder>] [-CE17 <dll>]
#        [-Workshop <folder>]
param(
    [ValidateSet("all", "1.6", "1.7")][string]$Version = "all",
    [string]$Out = "",
    [string]$RW = "F:\Steam\steamapps\common\RimWorld",
    [string]$CE17 = "",
    [string]$Workshop = "F:\Steam\steamapps\workshop\content\294100"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$frameworks = @{ "1.6" = "net472"; "1.7" = "net10.0" }
$versions = if ($Version -eq "all") { @("1.6", "1.7") } else { @($Version) }
# MSBuild would resolve a relative path against each project folder
if ($Out) { $Out = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out) }
if ($versions -contains "1.7" -and -not $CE17) {
    if (-not (Test-Path "$RW\UnityStubs.dll")) {
        throw "$RW is not a RimWorld 1.7 folder (no UnityStubs.dll), needed for the 1.7 facades: pass -RW <1.7 folder>, or -Version 1.6"
    }
    # Mono.Cecil from the Harmony 1.6 build reads the UnityStubs type list
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$root\tools\unity_facades.ps1" -RW $RW -Out "$root\obj\facades" `
        -Cecil "$Workshop\2009463077\1.6\Assemblies\0Harmony.dll"
    if ($LASTEXITCODE -ne 0) { throw "facades failed ($LASTEXITCODE)" }
}
$props = @("-p:Workshop=$Workshop")
if ($Out) { $props += "-p:ModBin=$Out" }
if ($CE17) { $props += "-p:CombatExtended17=$CE17"; $props += "-p:CEFacades=false" }
foreach ($v in $versions) {
    & dotnet build "$root\Source\MountedArmsStrategic.csproj" -c Release -f $frameworks[$v] --nologo -v minimal @props
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($v)" }
}
$bin = (Get-Item $(if ($Out) { $Out } else { "$root\Bin" })).FullName
Get-ChildItem $bin -Recurse -Filter *.dll | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm MD5).Hash, $_.FullName.Substring($bin.Length).TrimStart([char]'\')
}
