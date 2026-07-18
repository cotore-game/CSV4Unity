param(
    [string]$UnityEditorContents = "C:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Data"
)

$ErrorActionPreference = "Stop"
$env:UnityEditorContents = $UnityEditorContents

Push-Location $PSScriptRoot
try {
    dotnet restore CSV4Unity.Docs.csproj
    docfx metadata docfx.json
    docfx build docfx.json
    Write-Host "CSV4Unity API site generated at: $PSScriptRoot\_site\index.html"
}
finally {
    Pop-Location
}
