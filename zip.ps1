# Zip-Clean.ps1 - zip current directory excluding specified folders with quiet colon progress bar

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ZipName = "project_clean.zip"
$ExcludeDirs = @("bin", "obj", "venv", ".git", "node_modules", "__pycache__", ".vs", ".vscode", ".github", ".next",".idea","logs","dist")

# One-liner header only
Write-Host "Creating zip excluding: $($ExcludeDirs -join ', ')`n"

# Collect files quietly
$Files = Get-ChildItem -Recurse -File | Where-Object {
    $exclude = $false
    foreach ($dir in $ExcludeDirs) {
        if ($_.FullName -match "\\$dir\\") { $exclude = $true; break }
    }
    -not $exclude
}

$total = $Files.Count
if ($total -eq 0) {
    Write-Host "⚠️  No files found to zip."
    Pause
    exit
}

# Prep zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $ZipName) { Remove-Item $ZipName -Force }

$Zip = [System.IO.Compression.ZipFile]::Open($ZipName, 'Create')

# Helper: stable relative path (no Resolve-Path noise)
$base = (Get-Location).Path
function Get-RelPath([string]$full, [string]$base) {
    if ($full.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($base.Length + 1)
    }
    # Fallback (rare)
    try { return (Resolve-Path -LiteralPath $full -Relative) } catch { return [IO.Path]::GetFileName($full) }
}

# Progress bar config (ASCII colons)
$barLen = 30
$skipped = 0
$start = Get-Date
$lastDraw = Get-Date  # to avoid too-frequent redraws

for ($i = 0; $i -lt $total; $i++) {
    $f = $Files[$i]
    $rel = Get-RelPath $f.FullName $base

    try {
        # Zip without emitting objects
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $f.FullName, $rel)
    } catch {
        # Skip locked/unsupported/long-path issues; keep going
        $skipped++
    }

    # Draw progress (throttle to ~30fps)
    $percent = [int](($i + 1) * 100 / $total)
    $filled = [int]([math]::Round($barLen * $percent / 100.0))
    if ($filled -gt $barLen) { $filled = $barLen }
    $bar = (':' * $filled).TrimStart().PadRight($barLen, ' ')
    $now = Get-Date
    if (($now - $lastDraw).TotalMilliseconds -ge 33 -or $i -eq $total - 1) {
        Write-Host -NoNewline "`rZipping files [$bar] $percent% ($($i+1)/$total)"
        $lastDraw = $now
    }
}

$Zip.Dispose()

$elapsed = (Get-Date) - $start
Write-Host "`n✅ Done! Created: $ZipName ($($total-$skipped) files zipped, $skipped skipped) in $([math]::Round($elapsed.TotalSeconds,1))s"
Pause
