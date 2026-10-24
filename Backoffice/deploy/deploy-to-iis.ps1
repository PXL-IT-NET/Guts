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

# Ensure destination directory exists
if (-not (Test-Path $DestinationPath)) {
    Write-Host "Creating destination directory: $DestinationPath"
    New-Item -ItemType Directory -Force -Path $DestinationPath | Out-Null
}

$appCmd = "$env:SystemRoot\system32\inetsrv\appcmd.exe"

function Invoke-AppCmd($arguments) {
    if (Test-Path $appCmd) {
        & $appCmd $arguments
    }
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

# Step 2: Stop AppPool if appcmd is available
if (Test-Path $appCmd) {
    Write-Host "Stopping IIS Application Pool '$AppPoolName'..."
    Invoke-AppCmd @("stop", "apppool", "/apppool.name:$AppPoolName") 2>$null
}

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
        Remove-Item -Path $tempBackupDir -Recurse -Force
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
    # Robocopy exit codes: 0-7 = Success, >=8 = Failure
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
    if ($rc -ge 8) {
        throw "Robocopy failed with exit code $rc"
    }
    Write-Host "Files copied successfully (Robocopy code $rc)."

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
    }
}
finally {
    # Step 7: Bring application back online
    if (Test-Path $appOfflinePath) {
        Write-Host "Removing app_offline.htm..."
        Remove-Item -Path $appOfflinePath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $appCmd) {
        Write-Host "Starting IIS Application Pool '$AppPoolName'..."
        Invoke-AppCmd @("start", "apppool", "/apppool.name:$AppPoolName") 2>$null
    }
}

Write-Host "Deployment of $AppType completed successfully."

