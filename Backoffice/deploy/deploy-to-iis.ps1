[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Api", "Web")]
    [string]$AppType,

    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $false)]
    [string]$DestinationPath,

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName = "StagingPool",

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentName = "Staging"
)

$ErrorActionPreference = "Stop"

if (-not $DestinationPath) {
    if ($AppType -eq "Api") {
        $DestinationPath = "C:\Guts_Staging\Guts-Api"
    } else {
        $DestinationPath = "C:\Guts_Staging\Guts-Web"
    }
}

Write-Host "=========================================="
Write-Host "Deploying $AppType ($EnvironmentName)"
Write-Host "Source:      $SourcePath"
Write-Host "Destination: $DestinationPath"
Write-Host "AppPool:     $AppPoolName"
Write-Host "=========================================="

if (-not (Test-Path $SourcePath)) {
    throw "Source directory does not exist: $SourcePath"
}

$sourceFiles = Get-ChildItem -Path $SourcePath
Write-Host "Found $($sourceFiles.Count) items in source directory."
if ($sourceFiles.Count -eq 0) {
    throw "Source directory is empty: $SourcePath"
}

# Ensure destination directory exists
if (-not (Test-Path $DestinationPath)) {
    Write-Host "Creating destination directory: $DestinationPath"
    New-Item -ItemType Directory -Force -Path $DestinationPath | Out-Null
}

$appCmd = "$env:SystemRoot\system32\inetsrv\appcmd.exe"

# Import WebAdministration if available
if (Get-Module -ListAvailable -Name WebAdministration) {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
}

function Stop-Pool($poolName) {
    Write-Host "Stopping Application Pool '$poolName'..."
    try {
        if (Get-Command Stop-WebAppPool -ErrorAction SilentlyContinue) {
            Stop-WebAppPool -Name $poolName -ErrorAction SilentlyContinue
            Write-Host "Stop-WebAppPool executed for '$poolName'."
        } elseif (Test-Path $appCmd) {
            & $appCmd stop apppool "/apppool.name:$poolName" 2>$null
            Write-Host "appcmd stop apppool executed for '$poolName'."
        }
    } catch {
        Write-Warning "Notice when stopping AppPool '$poolName': $_"
    }
    $global:LASTEXITCODE = 0
}

function Start-Pool($poolName) {
    Write-Host "Starting Application Pool '$poolName'..."
    try {
        if (Get-Command Start-WebAppPool -ErrorAction SilentlyContinue) {
            Start-WebAppPool -Name $poolName -ErrorAction SilentlyContinue
            Write-Host "Start-WebAppPool executed for '$poolName'."
        } elseif (Test-Path $appCmd) {
            & $appCmd start apppool "/apppool.name:$poolName" 2>$null
            Write-Host "appcmd start apppool executed for '$poolName'."
        }
    } catch {
        Write-Warning "Notice when starting AppPool '$poolName': $_"
    }
    $global:LASTEXITCODE = 0
}

# Step 1: Place app_offline.htm to instruct ASP.NET Core Module to release file locks
$appOfflinePath = Join-Path $DestinationPath "app_offline.htm"
$appOfflineContent = @"
<!DOCTYPE html>
<html>
<head><title>Application Updating</title></head>
<body><h2>Updating GUTS Backoffice ($AppType)...</h2><p>Please wait a moment while the new version is being deployed.</p></body>
</html>
"@

Write-Host "Taking application offline via app_offline.htm..."
Set-Content -Path $appOfflinePath -Value $appOfflineContent -Force

# Step 2: Stop AppPool
Stop-Pool $AppPoolName

Start-Sleep -Seconds 3

try {
    # Step 3: Preserve environment config and logs
    $preservedFiles = @(
        "appsettings.$EnvironmentName.json",
        "appsettings.Production.json",
        "appsettings.Staging.json"
    )

    $tempBackupDir = Join-Path $env:TEMP "guts_backup_$AppType"
    if (Test-Path $tempBackupDir) {
        Remove-Item -Path $tempBackupDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $tempBackupDir -Force | Out-Null

    foreach ($file in $preservedFiles) {
        $filePath = Join-Path $DestinationPath $file
        if (Test-Path $filePath) {
            Write-Host "Backing up existing config: $file"
            Copy-Item -Path $filePath -Destination $tempBackupDir -Force
        }
    }

    # Step 4: Copy new files using Robocopy
    Write-Host "Copying files from $SourcePath to $DestinationPath..."
    # Robocopy exit codes: 0-7 = Success (0 = No change, 1 = Files copied, etc.), >=8 = Failure
    $robocopyArgs = @(
        $SourcePath,
        $DestinationPath,
        "/E",
        "/NP",
        "/R:3",
        "/W:2",
        "/XF", "app_offline.htm",
        "/XD", "Log", "logs"
    )
    & robocopy @robocopyArgs
    $rc = $LASTEXITCODE

    # Crucial: Reset LASTEXITCODE so PowerShell / GitHub Actions does not treat robocopy exit code (1-7) as an error!
    $global:LASTEXITCODE = 0

    if ($rc -ge 8) {
        throw "Robocopy failed with exit code $rc"
    }
    Write-Host "Files copied successfully (Robocopy exit code: $rc)."

    # Step 5: Restore preserved configs
    foreach ($file in $preservedFiles) {
        $backupFile = Join-Path $tempBackupDir $file
        if (Test-Path $backupFile) {
            Write-Host "Restoring preserved config: $file"
            Copy-Item -Path $backupFile -Destination $DestinationPath -Force
        }
    }

    # Step 6: Update web.config environment variable
    $webConfigPath = Join-Path $DestinationPath "web.config"
    if (Test-Path $webConfigPath) {
        try {
            [xml]$webConfig = Get-Content $webConfigPath
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
        } catch {
            Write-Warning "Could not update ASPNETCORE_ENVIRONMENT in web.config: $_"
        }
    }
}
finally {
    # Step 7: Bring application back online
    if (Test-Path $appOfflinePath) {
        Write-Host "Removing app_offline.htm..."
        Remove-Item -Path $appOfflinePath -Force -ErrorAction SilentlyContinue
    }

    Start-Pool $AppPoolName
    $global:LASTEXITCODE = 0
}

Write-Host "Deployment of $AppType completed successfully."
$global:LASTEXITCODE = 0
exit 0
