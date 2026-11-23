[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Api", "Web")]
    [string]$AppType,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SourcePath,

    [Parameter(Mandatory = $false)]
    [string]$DestinationPath,

    [Parameter(Mandatory = $false)]
    [ValidatePattern('^$|^[A-Za-z0-9._-]+$')]
    [string]$AppPoolName,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Staging", "Production")]
    [string]$EnvironmentName = "Staging",

    [Parameter(Mandatory = $false)]
    [switch]$SkipAppPoolControl
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($AppPoolName)) {
    if ($EnvironmentName -eq "Production") {
        $AppPoolName = if ($AppType -eq "Api") { "ApiCorePool" } else { "WebCorePool" }
    } else {
        $AppPoolName = if ($AppType -eq "Api") { "StagingApiCorePool" } else { "StagingWebCorePool" }
    }
}

$expectedDestination = if ($EnvironmentName -eq "Production") {
    if ($AppType -eq "Api") { "C:\Guts\Guts-Api" } else { "C:\Guts\Guts-Web" }
} else {
    if ($AppType -eq "Api") { "C:\Guts_Staging\Guts-Api" } else { "C:\Guts_Staging\Guts-Web" }
}

if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
    $DestinationPath = $expectedDestination
}

# Canonicalize paths and enforce strict, unbypassable destination allowlist
$resolvedSource = [System.IO.Path]::GetFullPath($SourcePath).TrimEnd('\')
$resolvedDestination = [System.IO.Path]::GetFullPath($DestinationPath).TrimEnd('\')
$allowedDestination = [System.IO.Path]::GetFullPath($expectedDestination).TrimEnd('\')

if (-not $resolvedDestination.Equals($allowedDestination, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "DestinationPath '$resolvedDestination' is not allowed for EnvironmentName '$EnvironmentName' and AppType '$AppType' (expected '$allowedDestination')."
}

Write-Host "=========================================="
Write-Host "Deploying $AppType ($EnvironmentName)"
Write-Host "Source:      $resolvedSource"
Write-Host "Destination: $resolvedDestination"
Write-Host "AppPool:     $AppPoolName"
Write-Host "=========================================="

if (-not (Test-Path -LiteralPath $resolvedSource -PathType Container)) {
    throw "Source directory does not exist: $resolvedSource"
}

$sourceFiles = Get-ChildItem -LiteralPath $resolvedSource
Write-Host "Found $($sourceFiles.Count) items in source directory."
if ($sourceFiles.Count -eq 0) {
    throw "Source directory is empty: $resolvedSource"
}

if (-not (Test-Path -LiteralPath $resolvedDestination -PathType Container)) {
    Write-Host "Creating destination directory: $resolvedDestination"
    New-Item -ItemType Directory -Force -Path $resolvedDestination | Out-Null
}

$isAdministrator = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
$manageAppPool = (-not $SkipAppPoolControl) -and $isAdministrator
$appCmd = "$env:SystemRoot\system32\inetsrv\appcmd.exe"

if ($manageAppPool) {
    if (Get-Module -ListAvailable -Name WebAdministration) {
        Import-Module WebAdministration -ErrorAction Stop
    }
} else {
    Write-Host "Running without IIS AppPool admin control (using app_offline.htm for ASP.NET Core Module shutdown/recycle)."
}

function Stop-Pool([string]$poolName) {
    Write-Output "Stopping Application Pool '$poolName'..."
    if (Get-Command Stop-WebAppPool -ErrorAction SilentlyContinue) {
        $state = (Get-WebAppPoolState -Name $poolName -ErrorAction Stop).Value
        if ($state -ne "Stopped") {
            Stop-WebAppPool -Name $poolName -ErrorAction Stop
            Write-Output "Stop-WebAppPool executed for '$poolName'."
        } else {
            Write-Output "Application Pool '$poolName' is already stopped."
        }
    } elseif (Test-Path -LiteralPath $appCmd) {
        $output = & $appCmd stop apppool "/apppool.name:$poolName" 2>&1
        $exitCode = $LASTEXITCODE
        $global:LASTEXITCODE = 0
        # appcmd returns 1062 when the pool is already stopped
        if ($exitCode -ne 0 -and $exitCode -ne 1062 -and ($output -notmatch "already stopped")) {
            throw "appcmd stop apppool failed for '$poolName' (exit code $exitCode): $output"
        }
        Write-Output "appcmd stop apppool completed for '$poolName'."
    } else {
        throw "Neither WebAdministration nor appcmd.exe is available to stop AppPool '$poolName'."
    }
}

function Start-Pool([string]$poolName) {
    Write-Output "Starting Application Pool '$poolName'..."
    if (Get-Command Start-WebAppPool -ErrorAction SilentlyContinue) {
        $state = (Get-WebAppPoolState -Name $poolName -ErrorAction Stop).Value
        if ($state -ne "Started") {
            Start-WebAppPool -Name $poolName -ErrorAction Stop
            Write-Output "Start-WebAppPool executed for '$poolName'."
        } else {
            Write-Output "Application Pool '$poolName' is already started."
        }
    } elseif (Test-Path -LiteralPath $appCmd) {
        $output = & $appCmd start apppool "/apppool.name:$poolName" 2>&1
        $exitCode = $LASTEXITCODE
        $global:LASTEXITCODE = 0
        # appcmd returns 1056 when the pool is already started
        if ($exitCode -ne 0 -and $exitCode -ne 1056 -and ($output -notmatch "already started")) {
            throw "appcmd start apppool failed for '$poolName' (exit code $exitCode): $output"
        }
        Write-Output "appcmd start apppool completed for '$poolName'."
    } else {
        throw "Neither WebAdministration nor appcmd.exe is available to start AppPool '$poolName'."
    }
}

# Step 1: Place app_offline.htm so ASP.NET Core Module cleanly shuts down the process and releases file locks
$appOfflinePath = Join-Path $resolvedDestination "app_offline.htm"
$appOfflineContent = @"
<!DOCTYPE html>
<html>
<head><title>Application Updating</title></head>
<body><h2>Updating GUTS Backoffice ($AppType)...</h2><p>Please wait a moment while the new version is being deployed.</p></body>
</html>
"@

Write-Host "Taking application offline via app_offline.htm..."
Set-Content -LiteralPath $appOfflinePath -Value $appOfflineContent -Encoding UTF8 -Force

# Step 2: Stop AppPool when elevated, or wait for ASP.NET Core Module graceful shutdown
if ($manageAppPool) {
    Stop-Pool $AppPoolName
    Start-Sleep -Seconds 3
} else {
    Start-Sleep -Seconds 5
}

try {
    # Step 3: Mirror new files directly with Robocopy while excluding environment configs and logs
    # Preserved configs remain in place and are NEVER copied to %TEMP% (prevents secret exposure).
    $excludedFiles = @(
        "app_offline.htm",
        "appsettings.$EnvironmentName.json",
        "appsettings.Production.json",
        "appsettings.Staging.json",
        "appsettings.Local.json"
    ) | Select-Object -Unique

    Write-Host "Mirroring files from $resolvedSource to $resolvedDestination (preserving environment configs and Log/logs)..."
    # Robocopy exit codes: 0-7 = Success, >=8 = Failure
    $robocopyArgs = @(
        $resolvedSource,
        $resolvedDestination,
        "/MIR",
        "/NP",
        "/R:3",
        "/W:2",
        "/XF"
    ) + $excludedFiles + @(
        "/XD", "Log", "logs"
    )

    & robocopy @robocopyArgs
    $rc = $LASTEXITCODE
    $global:LASTEXITCODE = 0

    if ($rc -ge 8) {
        throw "Robocopy failed with exit code $rc"
    }
    Write-Host "Files mirrored successfully (Robocopy exit code: $rc)."

    # Step 4: Update web.config ASPNETCORE_ENVIRONMENT variable
    $webConfigPath = Join-Path $resolvedDestination "web.config"
    if (Test-Path -LiteralPath $webConfigPath -PathType Leaf) {
        [xml]$webConfig = Get-Content -LiteralPath $webConfigPath
        $aspNetCore = $webConfig.SelectSingleNode("//aspNetCore")
        if ($aspNetCore) {
            $envVars = $aspNetCore.SelectSingleNode("environmentVariables")
            if (-not $envVars) {
                $envVars = $webConfig.CreateElement("environmentVariables")
                $aspNetCore.AppendChild($envVars) | Out-Null
            }
            $envVar = $envVars.SelectSingleNode("environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")
            if (-not $envVar) {
                $envVar = $webConfig.CreateElement("environmentVariable")
                $envVar.SetAttribute("name", "ASPNETCORE_ENVIRONMENT")
                $envVars.AppendChild($envVar) | Out-Null
            }
            $envVar.SetAttribute("value", $EnvironmentName)
            $webConfig.Save($webConfigPath)
            Write-Host "Configured ASPNETCORE_ENVIRONMENT=$EnvironmentName in web.config."
        }
    }
}
finally {
    # Step 5: Bring application back online
    if (Test-Path -LiteralPath $appOfflinePath -PathType Leaf) {
        Write-Host "Removing app_offline.htm..."
        Remove-Item -LiteralPath $appOfflinePath -Force -ErrorAction SilentlyContinue
    }

    if ($manageAppPool) {
        Start-Pool $AppPoolName
    }
    $global:LASTEXITCODE = 0
}

Write-Host "Deployment of $AppType completed successfully."
$global:LASTEXITCODE = 0
exit 0
