# Builds forwarder assemblies for compiling against RimWorld 1.7 while Combat Extended is still the 1.6 build.
# CE references UnityEngine.CoreModule/IMGUIModule/...; 1.7 has no Unity and only redirects those names to
# UnityStubs at run time (RimWorld.runtimeconfig.json AssemblyNameRedirects). The compiler does not know that
# redirect (CS0012), so each facade forwards every public UnityEngine*/Unity.Mathematics type to UnityStubs.
# usage: powershell -File tools/unity_facades.ps1 -RW <RimWorld 1.7 folder> -Out <folder>
param(
    [string]$RW = "F:\Steam\steamapps\common\RimWorld",
    [string]$Out = "$PSScriptRoot\..\obj\facades",
    # Mono.Cecil (bundled in Harmony 2) reads the UnityStubs type list; PowerShell 5 cannot load a .NET 10 DLL
    [string]$Cecil = "F:\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies\0Harmony.dll"
)
$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
New-Item -ItemType Directory -Force $Out | Out-Null

$h = [Reflection.Assembly]::LoadFile($Cecil)
$readerParams = [Activator]::CreateInstance($h.GetType("Mono.Cecil.ReaderParameters"))
$read = $h.GetType("Mono.Cecil.AssemblyDefinition").GetMethod("ReadAssembly", [Type[]]@([string], $h.GetType("Mono.Cecil.ReaderParameters")))
$stubs = $read.Invoke($null, @("$RW\UnityStubs.dll", $readerParams))
$lines = @("using System.Runtime.CompilerServices;")
foreach ($t in $stubs.MainModule.Types) {
    # Unity.Collections/Burst/Jobs hold unmanaged generics C# 5 cannot name; CE does not use them in signatures
    if (-not $t.IsPublic -or -not ($t.Namespace.StartsWith("UnityEngine") -or $t.Namespace -eq "Unity.Mathematics")) { continue }
    $name = $t.FullName.Split('`')[0]
    $arity = $t.GenericParameters.Count
    if ($arity -gt 0) { $name += "<" + ("," * ($arity - 1)) + ">" }
    $lines += "[assembly: TypeForwardedTo(typeof(global::$name))]"
}
$src = Join-Path $Out "UnityForwards.cs"
$lines | Set-Content -Encoding utf8 $src

foreach ($n in @("UnityEngine", "UnityEngine.CoreModule", "UnityEngine.IMGUIModule", "UnityEngine.TextRenderingModule", "UnityEngine.InputLegacyModule", "UnityEngine.UI")) {
    & $csc /nologo /noconfig /nostdlib /target:library /out:"$Out\$n.dll" `
        /r:"$RW\mscorlib.dll" /r:"$RW\netstandard.dll" /r:"$RW\System.Runtime.dll" /r:"$RW\System.Private.CoreLib.dll" `
        /r:"$RW\UnityStubs.dll" /r:"$RW\Waylith.dll" $src
    if ($LASTEXITCODE -ne 0) { throw "csc failed for facade $n ($LASTEXITCODE)" }
}
