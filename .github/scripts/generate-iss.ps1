<#
.SYNOPSIS
    .iss テンプレートからバージョン等を置換して Inno Setup スクリプトを生成します。
#>
param(
    [Parameter(Mandatory)] [string]$Version,
    [Parameter(Mandatory)] [string]$RepoUrl,
    [Parameter(Mandatory)] [string]$AppName,
    [Parameter(Mandatory)] [string]$AppExeName,
    [Parameter(Mandatory)] [string]$AppId,
    [Parameter(Mandatory)] [string]$AppPublisher,
    [string]$TemplatePath = ".github/inno/installer.iss.template",
    [string]$OutputPath = "installer-script.iss"
)

if (-not (Test-Path $TemplatePath)) {
    Write-Error "Template not found: $TemplatePath"
    exit 1
}

# テンプレートを UTF-8 で読み込む
$content = Get-Content -Path $TemplatePath -Raw -Encoding UTF8

# プレースホルダーを実際の値に置換
$content = $content -replace '\{\{APP_VERSION\}\}', $Version
$content = $content -replace '\{\{REPO_URL\}\}', $RepoUrl
$content = $content -replace '\{\{REPO_ISSUES_URL\}\}', "$RepoUrl/issues"
$content = $content -replace '\{\{REPO_RELEASES_URL\}\}', "$RepoUrl/releases"
$content = $content -replace '\{\{APP_NAME\}\}', $AppName
$content = $content -replace '\{\{APP_EXE_NAME\}\}', $AppExeName
$content = $content -replace '\{\{APP_ID\}\}', $AppId
$content = $content -replace '\{\{APP_PUBLISHER\}\}', $AppPublisher

$outputDir = Split-Path $OutputPath -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

# UTF-8 BOM付きで書き出す（Inno Setup 6 は BOM付き UTF-8 を正しく認識する）
$content | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Host "Generated: $OutputPath"
Write-Host "  App:     $AppName ($AppExeName.exe)"
Write-Host "  Version: $Version"
