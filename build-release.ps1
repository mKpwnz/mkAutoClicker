$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectFile = Join-Path $PSScriptRoot "mkAutoClicker.csproj"
if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Project file not found: $projectFile"
}

$processNames = @("mkClickerWpfSingle", "mkAutoClicker")
foreach ($processName in $processNames) {
    $running = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($null -ne $running) {
        $running | Stop-Process -Force
    }
}

[xml]$projectXml = Get-Content -LiteralPath $projectFile
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = $projectXml.Project.PropertyGroup.VersionPrefix | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "0.0.0"
}

$publishRoot = Join-Path $PSScriptRoot "bin\publish\win-x64\framework-dependent-onefile"
if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

Write-Host "Publish: framework-dependent single-file (runtime required)..."
& dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    -p:SelfContained=false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:PublishTrimmed=false `
    -o $publishRoot

$sourceExe = Join-Path $publishRoot "mkClickerWpfSingle.exe"
if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Expected output executable not found: $sourceExe"
}

$versionedExeName = "mkAutoClicker_{0}.exe" -f $version
$versionedExePath = Join-Path $publishRoot $versionedExeName
if (Test-Path -LiteralPath $versionedExePath) {
    Remove-Item -LiteralPath $versionedExePath -Force
}

Rename-Item -LiteralPath $sourceExe -NewName $versionedExeName

Get-ChildItem -LiteralPath $publishRoot -File |
    Where-Object { $_.Name -ne $versionedExeName } |
    Remove-Item -Force

Write-Host ""
Write-Host "Done. Artifact:"
Write-Host "- $versionedExePath"
