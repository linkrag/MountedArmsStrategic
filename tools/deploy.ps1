# Mirrors ONLY the public part of the mod (what the game loads and what goes to the Steam Workshop) into the game's
# Mods folder: About/, LoadFolders.xml, 1.6/ and Bin/. Everything else (Source, Docs, tools, .git, obj, AGENTS.md,
# build.ps1...) stays here in _dev\src and never reaches the Workshop, because the in-game uploader sends the whole
# Mods\<Mod> folder with no exclusion list (verified in Verse.Steam.Workshop, 1.7).
# build.ps1 calls this at the end (unless -Out). Run it alone after XML/texture/Keyed-only changes.
# Identical copy in every mod repo (tools/deploy.ps1): keep them in sync.
# usage: powershell -File tools\deploy.ps1 [-Mods <game Mods folder>]
param(
    [string]$Mods = "F:\Steam\steamapps\common\RimWorld\Mods"
)
$ErrorActionPreference = "Stop"
$src = (Get-Item "$PSScriptRoot\..").FullName
$name = Split-Path $src -Leaf
$dst = Join-Path $Mods $name
$dirs = @("About", "1.6", "Bin")
$files = @("LoadFolders.xml")
if (-not (Test-Path "$src\About\About.xml")) { throw "$src is not a mod folder (no About\About.xml)" }
New-Item -ItemType Directory -Force $dst | Out-Null

# The game writes About\PublishedFileId.txt into the Mods copy on the first Workshop upload: bring it back first,
# otherwise the mirror below would delete it and the next upload would create a duplicate Workshop item.
$pfDst = "$dst\About\PublishedFileId.txt"; $pfSrc = "$src\About\PublishedFileId.txt"
if ((Test-Path $pfDst) -and (-not (Test-Path $pfSrc) -or (Get-Content $pfDst -Raw) -ne (Get-Content $pfSrc -Raw))) {
    Copy-Item $pfDst $pfSrc -Force
    Write-Host "PublishedFileId.txt copied back from $dst"
}

foreach ($d in $dirs) {
    if (-not (Test-Path "$src\$d")) { continue }
    # /MIR: exact copy (deletes stale files); robocopy exit codes 0-7 are success
    & robocopy "$src\$d" "$dst\$d" /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:1 /XF Thumbs.db .DS_Store *.pdb | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $d ($LASTEXITCODE): is the game open (locked DLL)?" }
}
foreach ($f in $files) { if (Test-Path "$src\$f") { Copy-Item "$src\$f" "$dst\$f" -Force } }

# anything else at the top level of the Mods copy is not public: remove it
$keep = $dirs + $files
Get-ChildItem $dst -Force | Where-Object { $keep -notcontains $_.Name } | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
    Write-Host "removed from Mods copy: $($_.Name)"
}
$n = (Get-ChildItem $dst -Recurse -File).Count
$mb = [math]::Round(((Get-ChildItem $dst -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 2)
Write-Host "deployed $name -> $dst ($n files, $mb MB)"
