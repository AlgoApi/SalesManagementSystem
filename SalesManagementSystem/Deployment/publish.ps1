param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Output = ".\publish"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\SalesManagementSystem.csproj"

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output

Write-Host "Published to $Output"
