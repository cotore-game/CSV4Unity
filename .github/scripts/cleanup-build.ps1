<#
.SYNOPSIS
    Unity ビルド後の開発専用フォルダを削除します。
#>

param(
    [Parameter(Mandatory)]
    [string]$BuildPath
)

if (-not (Test-Path $BuildPath)) {
    Write-Error "BuildPath not found: $BuildPath"
    exit 1
}

$patterns = @(
    "*DoNotShip*",
    "*_BackUpThisFolder_ButDontShipItWithYourGame*"
)

$removed = 0

foreach ($pattern in $patterns) {
    Get-ChildItem -Path $BuildPath -Recurse -Directory |
        Where-Object { $_.Name -like $pattern } |
        ForEach-Object {
            Write-Host "Removing: $($_.FullName)"
            Remove-Item -Path $_.FullName -Recurse -Force
            $removed++
        }
}

if ($removed -eq 0) {
    Write-Host "No target folders found for cleanup."
} else {
    Write-Host "Cleanup complete. Removed $removed folders."
}
