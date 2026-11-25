$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$localSdkRoot = Join-Path $repositoryRoot '.local\dotnet-sdk'
$dotnetExecutable = Join-Path $localSdkRoot 'dotnet.exe'

if (-not (Test-Path -LiteralPath $dotnetExecutable)) {
    $installedDotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if (-not $installedDotnet) {
        throw 'Install .NET SDK 11 to build this client.'
    }
    $dotnetExecutable = $installedDotnet.Source
}

$environmentNames = @('DOTNET_ROOT', 'DOTNET_CLI_HOME', 'NUGET_PACKAGES', 'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_NOLOGO')
$previousEnvironment = @{}
foreach ($environmentName in $environmentNames) {
    $previousEnvironment[$environmentName] = [Environment]::GetEnvironmentVariable($environmentName, 'Process')
}

Push-Location $PSScriptRoot
try {
    $env:DOTNET_ROOT = Split-Path -Parent $dotnetExecutable
    $env:DOTNET_CLI_HOME = Join-Path $repositoryRoot '.local\dotnet'
    $env:NUGET_PACKAGES = Join-Path $repositoryRoot '.local\nuget\packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    & $dotnetExecutable @args
    $commandExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
    foreach ($environmentName in $environmentNames) {
        [Environment]::SetEnvironmentVariable($environmentName, $previousEnvironment[$environmentName], 'Process')
    }
}
exit $commandExitCode
