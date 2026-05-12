param(
    [ValidateSet("tiny", "onefile")]
    [string]$Mode = "tiny"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectFile = Join-Path $PSScriptRoot "mkAutoClicker.csproj"
if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Projektdatei nicht gefunden: $projectFile"
}

$processNames = @("mkClickerWpfSingle", "mkAutoClicker")
foreach ($processName in $processNames) {
    $running = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($null -ne $running) {
        $running | Stop-Process -Force
    }
}

$publishRoot = Join-Path $PSScriptRoot "bin\publish\win-x64"
$targetDirName = if ($Mode -eq "onefile") { "framework-dependent-onefile" } else { "framework-dependent-tiny" }
$outputDir = Join-Path $publishRoot $targetDirName
if (Test-Path -LiteralPath $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

if ($Mode -eq "onefile") {
    Write-Host "Publish: framework-dependent (single EXE, runtime required)..."
    & dotnet publish $projectFile `
        -c Release `
        -r win-x64 `
        -p:SelfContained=false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:PublishTrimmed=false `
        -o $outputDir
} else {
    Write-Host "Publish: framework-dependent (tiny, multi-file, runtime required)..."
    & dotnet publish $projectFile `
        -c Release `
        -r win-x64 `
        -p:SelfContained=false `
        -p:PublishSingleFile=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:PublishTrimmed=false `
        -o $outputDir
}

$exePath = Join-Path $outputDir "mkClickerWpfSingle.exe"
Write-Host ""
Write-Host "Fertig. Artefakt:"
Write-Host "- $exePath"
