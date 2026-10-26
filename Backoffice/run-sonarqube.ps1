<#
.SYNOPSIS
    Runs SonarQube or SonarCloud code quality and test coverage analysis for the GUTS Backoffice (.NET backend + Angular frontend).

.PARAMETER SonarToken
    SonarQube/SonarCloud authentication token. Defaults to the SONAR_TOKEN (or SONARCLOUD_TOKEN when -UseSonarCloud is set) environment variable or cached .sonar-token / .sonarcloud-token.

.PARAMETER SonarUser
    SonarQube username for automatic token generation when no token is cached (local SonarQube only). Defaults to "admin".

.PARAMETER SonarPassword
    SonarQube password for automatic token generation when no token is cached (local SonarQube only).

.PARAMETER SonarUrl
    SonarQube server URL. Defaults to http://localhost:9000 (or https://sonarcloud.io when -UseSonarCloud is set).

.PARAMETER ProjectKey
    SonarQube project key. Defaults to guts-backoffice (or PXL-IT-NET_Guts when -UseSonarCloud is set).

.PARAMETER ProjectName
    SonarQube project display name. Defaults to "GUTS Backoffice".

.PARAMETER Organization
    SonarCloud organization key (used when -UseSonarCloud is set). Defaults to "pxl-it-net".

.PARAMETER UseSonarCloud
    If set, runs the analysis against SonarCloud (https://sonarcloud.io, organization pxl-it-net, project PXL-IT-NET_Guts) using SonarCloud's analyzers.

.PARAMETER StartServer
    If set, starts the local SonarQube Docker containers (docker-compose.sonarqube.yml) and waits until the server is UP.

.PARAMETER SkipFrontendTests
    If set, skips running Angular unit tests (npm run test:coverage) before analysis.

.PARAMETER SkipBackendTests
    If set, skips running .NET unit tests before completing analysis.

.EXAMPLE
    .\run-sonarqube.ps1 -StartServer

.EXAMPLE
    .\run-sonarqube.ps1 -UseSonarCloud
#>
param(
    [string]$SonarToken = "",
    [string]$SonarUser = "admin",
    [string]$SonarPassword = "",
    [string]$SonarUrl = "",
    [string]$ProjectKey = "",
    [string]$ProjectName = "GUTS Backoffice",
    [string]$Organization = "pxl-it-net",
    [switch]$UseSonarCloud,
    [switch]$StartServer,
    [switch]$SkipFrontendTests,
    [switch]$SkipBackendTests
)

# Use Continue so Windows PowerShell 5.1 does not abort when native CLI tools (node/npm/dotnet) write warnings to stderr
$ErrorActionPreference = "Continue"
$BackofficeDir = $PSScriptRoot
$ClientAppDir = Join-Path $BackofficeDir "Guts.Web\ClientApp"
$TestResultsDir = Join-Path $BackofficeDir "TestResults"

if ($UseSonarCloud) {
    if ([string]::IsNullOrWhiteSpace($SonarUrl)) { $SonarUrl = "https://sonarcloud.io" }
    if ([string]::IsNullOrWhiteSpace($ProjectKey)) { $ProjectKey = "PXL-IT-NET_Guts" }
    if ([string]::IsNullOrWhiteSpace($SonarToken)) {
        $SonarToken = if (-not [string]::IsNullOrWhiteSpace($env:SONARCLOUD_TOKEN)) { $env:SONARCLOUD_TOKEN } else { $env:SONAR_TOKEN }
    }
    $TokenCacheFile = Join-Path $BackofficeDir ".sonarcloud-token"
}
else {
    if ([string]::IsNullOrWhiteSpace($SonarUrl)) { $SonarUrl = "http://localhost:9000" }
    if ([string]::IsNullOrWhiteSpace($ProjectKey)) { $ProjectKey = "guts-backoffice" }
    if ([string]::IsNullOrWhiteSpace($SonarToken)) { $SonarToken = $env:SONAR_TOKEN }
    $TokenCacheFile = Join-Path $BackofficeDir ".sonar-token"
}

Push-Location $BackofficeDir
try {
    if ($StartServer -and -not $UseSonarCloud) {
        Write-Host "==> Starting local SonarQube server via Docker Compose..." -ForegroundColor Cyan
        docker compose -f docker-compose.sonarqube.yml up -d
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose up failed with exit code $LASTEXITCODE."
        }

        Write-Host "==> Waiting for SonarQube at $SonarUrl to become ready..." -ForegroundColor Cyan
        $maxAttempts = 60
        $attempt = 0
        $isUp = $false
        while ($attempt -lt $maxAttempts) {
            $attempt++
            try {
                $statusResponse = Invoke-RestMethod -Uri "$SonarUrl/api/system/status" -Method Get -TimeoutSec 3 -ErrorAction Stop
                if ($statusResponse.status -eq "UP") {
                    $isUp = $true
                    break
                }
                Write-Host "    SonarQube status: $($statusResponse.status) (attempt $attempt/$maxAttempts)..." -ForegroundColor DarkGray
            }
            catch {
                Write-Host "    Waiting for SonarQube web server (attempt $attempt/$maxAttempts)..." -ForegroundColor DarkGray
            }
            Start-Sleep -Seconds 5
        }

        if (-not $isUp) {
            throw "SonarQube did not reach 'UP' status at $SonarUrl within the timeout period."
        }
        Write-Host "==> SonarQube is UP at $SonarUrl" -ForegroundColor Green
    }

    if ([string]::IsNullOrWhiteSpace($SonarToken) -and (Test-Path $TokenCacheFile)) {
        $SonarToken = (Get-Content $TokenCacheFile -Raw).Trim()
        if (-not [string]::IsNullOrWhiteSpace($SonarToken)) {
            Write-Host "==> Using cached token from $(Split-Path $TokenCacheFile -Leaf)" -ForegroundColor DarkGray
        }
    }

    if ([string]::IsNullOrWhiteSpace($SonarToken)) {
        if ($UseSonarCloud) {
            Write-Host "No SonarCloud token found. Generate one at https://sonarcloud.io/account/security and paste it below:" -ForegroundColor Yellow
            $SonarToken = Read-Host -Prompt "Paste your SonarCloud token"
        }
        else {
            if ([string]::IsNullOrWhiteSpace($SonarPassword)) {
                Write-Host "No SonarQube token found. Enter your SonarQube '$SonarUser' password to auto-generate a token (or press Enter to paste a token manually):" -ForegroundColor Yellow
                $SonarPassword = Read-Host -Prompt "SonarQube password for '$SonarUser'"
            }

            if (-not [string]::IsNullOrWhiteSpace($SonarPassword)) {
                Write-Host "==> Generating SonarQube analysis token via REST API..." -ForegroundColor Cyan
                $pair = "${SonarUser}:${SonarPassword}"
                $encodedCreds = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
                $headers = @{ Authorization = "Basic $encodedCreds" }
                $tokenName = "guts-local-$(Get-Date -Format 'yyyyMMddHHmmss')"
                $tokenResponse = Invoke-RestMethod -Uri "$SonarUrl/api/user_tokens/generate" -Method Post -Headers $headers -Body @{ name = $tokenName } -ErrorAction Stop
                $SonarToken = $tokenResponse.token
            }
            else {
                $SonarToken = Read-Host -Prompt "Paste your SonarQube token"
            }
        }

        if ([string]::IsNullOrWhiteSpace($SonarToken)) {
            throw "A Sonar token is required. Pass -SonarToken '<token>' or set `$env:SONAR_TOKEN / `$env:SONARCLOUD_TOKEN."
        }
    }

    $tokenDir = Split-Path $TokenCacheFile -Parent
    if (-not (Test-Path $tokenDir)) {
        New-Item -ItemType Directory -Path $tokenDir -Force | Out-Null
    }
    Set-Content -Path $TokenCacheFile -Value $SonarToken -NoNewline

    Write-Host "==> Restoring local .NET tools (dotnet-sonarscanner)..." -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    if (Test-Path $TestResultsDir) {
        Remove-Item -Path $TestResultsDir -Recurse -Force
    }

    if (-not $SkipFrontendTests) {
        Write-Host "==> Running Angular frontend unit tests with LCOV coverage..." -ForegroundColor Cyan
        Push-Location $ClientAppDir
        try {
            & npm.cmd run test:coverage
            if ($LASTEXITCODE -ne 0) {
                throw "Angular frontend tests failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }

    $lcovPath = Join-Path $ClientAppDir "coverage\ClientApp\lcov.info"
    if (Test-Path $lcovPath) {
        Write-Host "==> Normalizing LCOV source paths for SonarScanner..." -ForegroundColor Cyan
        $lcovLines = Get-Content -Path $lcovPath
        $normalizedLines = foreach ($line in $lcovLines) {
            if ($line.StartsWith("SF:")) {
                $sfPath = $line.Substring(3)
                if (-not [System.IO.Path]::IsPathRooted($sfPath)) {
                    $sfPath = Join-Path $ClientAppDir $sfPath
                }
                "SF:$sfPath"
            }
            else {
                $line
            }
        }
        Set-Content -Path $lcovPath -Value $normalizedLines -Encoding UTF8
    }

    $openCoverPaths = "$TestResultsDir/**/coverage.opencover.xml"
    $trxPaths = "$TestResultsDir/*.trx"

    $exclusions = @(
        "**/Migrations/**",
        "**/node_modules/**",
        "**/dist/**",
        "**/coverage/**",
        "**/TestResults/**",
        "**/*.spec.ts",
        "**/Guts.Web/ClientApp/src/environments/**",
        "**/Guts.Web/ClientApp/src/styles/sketchy/**",
        "**/Guts.Web/ClientApp/karma.conf.js",
        "**/Guts.Web/ClientApp/proxy.conf.js",
        "**/Guts.Web/ClientApp/aspnetcore-https.js"
    ) -join ","

    $coverageExclusions = @(
        "**/Guts.Bootstrapper/**",
        "**/*.ps1",
        "**/deploy/**"
    ) -join ","

    Write-Host "==> Starting SonarScanner analysis for '$ProjectKey' ($SonarUrl)..." -ForegroundColor Cyan
    $beginArgs = @(
        "tool", "run", "dotnet-sonarscanner", "begin",
        "/k:$ProjectKey",
        "/d:sonar.host.url=$SonarUrl",
        "/d:sonar.token=$SonarToken",
        "/d:sonar.cs.opencover.reportsPaths=$openCoverPaths",
        "/d:sonar.cs.vstest.reportsPaths=$trxPaths",
        "/d:sonar.javascript.lcov.reportPaths=$lcovPath",
        "/d:sonar.exclusions=$exclusions",
        "/d:sonar.coverage.exclusions=$coverageExclusions",
        "/d:sonar.test.inclusions=**/*.spec.ts"
    )
    if ($UseSonarCloud) {
        $beginArgs += "/o:$Organization"
    }
    else {
        $beginArgs += "/n:$ProjectName"
    }

    & dotnet @beginArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet-sonarscanner begin failed with exit code $LASTEXITCODE."
    }

    Write-Host "==> Building Backoffice solution (Guts.slnx)..." -ForegroundColor Cyan
    dotnet build Guts.slnx --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipBackendTests) {
        Write-Host "==> Running Backend unit tests with OpenCover coverage..." -ForegroundColor Cyan
        dotnet test Guts.slnx `
            --logger "trx" `
            --results-directory "$TestResultsDir" `
            --collect:"XPlat Code Coverage" `
            --settings "$BackofficeDir\coverlet.runsettings"

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "==> Completing SonarScanner analysis and uploading results..." -ForegroundColor Cyan
    dotnet tool run dotnet-sonarscanner end /d:sonar.token="$SonarToken"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet-sonarscanner end failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "==> Sonar analysis complete! View the report at:" -ForegroundColor Green
    Write-Host "    $SonarUrl/dashboard?id=$ProjectKey" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
