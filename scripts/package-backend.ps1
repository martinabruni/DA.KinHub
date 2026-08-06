[CmdletBinding()]
param(
    [string]$Environment = "Development",
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $root "src/backend/applications/DA.KinHub.Functions/DA.KinHub.Functions.csproj"
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $root "artifacts/backend"))
$publishPath = [System.IO.Path]::GetFullPath((Join-Path $artifactRoot "publish"))
if (-not $artifactRoot.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase) -or -not $publishPath.StartsWith($artifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Artifact paths escaped the repository root."
}

$version = (Get-Content -LiteralPath (Join-Path $root "VERSION") -Raw).Trim()
try { $sha = (git -C $root rev-parse --short=12 HEAD).Trim() } catch { $sha = "local" }
if ([string]::IsNullOrWhiteSpace($sha)) { $sha = "local" }
$buildDate = [DateTimeOffset]::UtcNow.ToString("O")
$artifactName = "kinhub-backend-$version-$sha"
$zipPath = Join-Path $artifactRoot "$artifactName.zip"
$checksumPath = "$zipPath.sha256"
$manifestPath = Join-Path $artifactRoot "build-manifest.json"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (Test-Path -LiteralPath $publishPath) { Remove-Item -LiteralPath $publishPath -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishPath | Out-Null
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

$properties = @(
    "-p:Version=$version",
    "-p:CommitSha=$sha",
    "-p:BuildDate=$buildDate",
    "-p:BuildEnvironment=$Environment",
    "-p:UseAppHost=false"
)

if (-not $SkipBuild) {
    dotnet restore $project
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
    dotnet build $project --configuration $Configuration --no-restore @properties
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}
if ($SkipBuild) {
    dotnet publish $project --configuration $Configuration --no-build --no-restore --output $publishPath @properties
} else {
    dotnet publish $project --configuration $Configuration --no-restore --output $publishPath @properties
}
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$required = @("host.json", "DA.KinHub.Functions.dll")
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishPath $file))) { throw "$file is missing from the publish root." }
}
$forbidden = Get-ChildItem -LiteralPath $publishPath -Recurse -File | Where-Object { $_.Name -eq "local.settings.json" -or $_.Name -like ".env*" }
if ($forbidden) { throw "The publish output contains forbidden local configuration files." }

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipPath -CompressionLevel Optimal
$checksum = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$checksum  $([System.IO.Path]::GetFileName($zipPath))" -Encoding utf8
@{
    appName = "KinHub"
    component = "backend"
    version = $version
    commitSha = $sha
    buildDate = $buildDate
    environment = $Environment
    artifact = [System.IO.Path]::GetFileName($zipPath)
    sha256 = $checksum
} | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Output $zipPath
