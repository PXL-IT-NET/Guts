<#
.SYNOPSIS
    Runs SonarQube code quality and test coverage analysis for the GUTS Backoffice (.NET backend + Angular frontend).

.PARAMETER SonarToken
    SonarQube authentication token. Defaults to the SONAR_TOKEN environment variable or cached .sonarqube\sonar-token.txt.

.PARAMETER SonarUser
    SonarQube username for automatic token generation when no token is cached. Defaults to "admin".

.PARAMETER SonarPassword
    SonarQube password for automatic token generation when no token is cached.

.PARAMETER SonarUrl
    SonarQube server URL. Defaults to http://localhost:9000.

.PARAMETER ProjectKey
    SonarQube project key. Defaults to guts-backoffice.

.PARAMETER ProjectName
    SonarQube project display name. Defaults to "GUTS Backoffice".

.PARAMETER StartServer
    If set, starts the local SonarQube Docker containers (docker-compose.sonarqube.yml) and waits until the server is UP.

.PARAMETER SkipFrontendTests
    If set, skips running Angular unit tests (npm run test:coverage) before analysis.

.PARAMETER SkipBackendTests
    If set, skips running .NET unit tests before completing analysis.

.EXAMPLE
    .\run-sonarqube.ps1
#>
param(
    [string]$SonarToken = $env:SONAR_TOKEN,
    [string]$SonarUser = "admin",
    [string]$SonarPassword = "",
    [string]$SonarUrl = "http://localhost:9000",
    [string]$ProjectKey = "guts-backoffice",
    [string]$ProjectName = "GUTS Backoffice",
    [switch]$StartServer,
    [switch]$SkipFrontendTests,
    [switch]$SkipBackendTests
)

# Use Continue so Windows PowerShell 5.1 does not abort when native CLI tools (node/npm/dotnet) write warnings to stderr
$ErrorActionPreference = "Continue"
$BackofficeDir = $PSScriptRoot
$ClientAppDir = Join-Path $BackofficeDir "Guts.Web\ClientApp"
$TestResultsDir = Join-Path $BackofficeDir "TestResults"
$TokenCacheFile = Join-Path $BackofficeDir ".sonar-token"

Push-Location $BackofficeDir
try {
    if ($StartServer) {
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
            Write-Host "==> Using cached SonarQube token from .sonar-token" -ForegroundColor DarkGray
        }
    }

    if ([string]::IsNullOrWhiteSpace($SonarToken)) {
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

        if ([string]::IsNullOrWhiteSpace($SonarToken)) {
            throw "A SonarQube token is required. Pass -SonarToken '<token>', -SonarPassword '<password>', or set `$env:SONAR_TOKEN."
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

    $lcovPath = "Guts.Web/ClientApp/coverage/ClientApp/lcov.info"
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
        "**/Guts.Web/ClientApp/karma.conf.js",
        "**/Guts.Web/ClientApp/proxy.conf.js",
        "**/Guts.Web/ClientApp/aspnetcore-https.js"
    ) -join ","

    Write-Host "==> Starting SonarScanner analysis for '$ProjectKey'..." -ForegroundColor Cyan
    dotnet tool run dotnet-sonarscanner begin `
        /k:"$ProjectKey" `
        /n:"$ProjectName" `
        /d:sonar.host.url="$SonarUrl" `
        /d:sonar.token="$SonarToken" `
        /d:sonar.cs.opencover.reportsPaths="$openCoverPaths" `
        /d:sonar.cs.vstest.reportsPaths="$trxPaths" `
        /d:sonar.javascript.lcov.reportPaths="$lcovPath" `
        /d:sonar.exclusions="$exclusions" `
        /d:sonar.test.inclusions="**/*.spec.ts"

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
            --no-build `
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
    Write-Host "==> SonarQube analysis complete! View the report at:" -ForegroundColor Green
    Write-Host "    $SonarUrl/dashboard?id=$ProjectKey" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
