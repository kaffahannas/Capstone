# Regenerate wwwroot/css/admin.css from component files.
$root = Split-Path $PSScriptRoot -Parent
$cssDir = Join-Path $root "wwwroot\css"
$files = @(
    "admin/components-admin-tokens.css",
    "admin/components-admin-sidebar.css",
    "admin/components-admin-topbar.css",
    "admin/components-admin-stat.css",
    "admin/components-admin-card.css",
    "admin/components-admin-table.css",
    "admin/components-admin-ui.css",
    "admin/components-admin-alert.css",
    "admin/components-admin-blocks.css",
    "admin/components-admin-misc.css",
    "admin/layout.css"
)
$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine("/* LightenUp Admin bundle - wwwroot/css/admin/components-admin-*.css + layout.css */")
[void]$out.AppendLine("/* Regenerate: scripts/bundle-admin-css.ps1 */")
[void]$out.AppendLine("")
foreach ($f in $files) {
    $path = Join-Path $cssDir $f
    if (-not (Test-Path $path)) { throw "Missing $path" }
    $content = Get-Content $path -Raw -Encoding UTF8
    $content = $content -replace "\uFEFF", ''
    [void]$out.AppendLine("/* === $f === */")
    [void]$out.AppendLine($content.TrimEnd())
    [void]$out.AppendLine("")
}
$dest = Join-Path $cssDir "admin.css"
[System.IO.File]::WriteAllText($dest, $out.ToString(), (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $dest"
