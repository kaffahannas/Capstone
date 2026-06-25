$files = Get-ChildItem "D:\LightenUp\WebsiteLightenUp\.claude\worktrees\pedantic-grothendieck-8c4b6d" -Recurse -Filter "*.cshtml" |
    Select-String "Â" | Select-Object -ExpandProperty Path -Unique

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
    # Â· = middle dot (·)
    $content = $content.Replace([char]0xC3 + [char]0x82 + [char]0xC2 + [char]0xB7, [char]0xC2 + [char]0xB7)
    # Fallback: literal string replacement
    $content = $content -replace 'Â·', '&middot;'
    [System.IO.File]::WriteAllText($file, $content, (New-Object System.Text.UTF8Encoding($false)))
    "Fixed: $(Split-Path $file -Leaf)" | Write-Host
}
"Done" | Write-Host
