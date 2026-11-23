<#
.SYNOPSIS
    Zero-install security pre-scanner for the GUTS repository (Backoffice .NET/Angular, DotNetClient, JavaClients, DevOps/CI).

.DESCRIPTION
    Runs native dependency audits (dotnet, npm, Gradle inspection), secret & repository hygiene checks,
    stack-specific high-risk code sink/misconfiguration pattern scans, and optional external scanners
    (trivy, gitleaks, semgrep, osv-scanner) when installed.
    Outputs a structured Markdown report to stdout without writing unpatched findings to Git.

.PARAMETER Scope
    Target audit scope: All, Backoffice, BackofficeDotNet, BackofficeAngular, DotNetClient, JavaClients, DevOps, Diff.

.PARAMETER BaseRef
    Git ref used when -Scope Diff is selected. Defaults to HEAD (scans working tree + staged changes; pass e.g. origin/main for branch diff).

.PARAMETER SkipNetworkChecks
    Skips online package vulnerability lookups (dotnet list package --vulnerable, npm audit) for offline/sandbox runs.

.PARAMETER IncludeExternalTools
    If set, invokes installed external security tools (trivy, gitleaks, semgrep, osv-scanner) when available on PATH.
#>
[CmdletBinding()]
param(
    [ValidateSet('All', 'Backoffice', 'BackofficeDotNet', 'BackofficeAngular', 'DotNetClient', 'JavaClients', 'DevOps', 'Diff')]
    [string]$Scope = 'All',

    [string]$BaseRef = 'HEAD',

    [switch]$SkipNetworkChecks,

    [switch]$IncludeExternalTools
)

$ErrorActionPreference = 'Continue'

# Resolve repository root relative to script location (.agent/skills/security-audit/scripts)
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..') | Select-Object -ExpandProperty Path

function Write-Section([string]$Title) {
    Write-Output ""
    Write-Output "## $Title"
    Write-Output ""
}

function Get-ScopedFiles {
    param([string]$CurrentScope)

    $excludedDirRegex = '(^|[\\/])(\.git|node_modules|bin|obj|dist|build|TestResults|playwright-report|coverage|\.gradle|\.angular|\.vs|\.sonarqube|\.sonarscanner|Log)([\\/]|$)'
    $excludedFileRegex = '(package-lock\.json|\.min\.js|\.min\.css|\.map|\.dll|\.exe|\.pdb|\.png|\.jpg|\.jpeg|\.gif|\.ico|\.woff2?|\.ttf|\.eot|\.jar|\.zip|\.pdf|\.pfx|\.snk)$'

    if ($CurrentScope -eq 'Diff') {
        $diffFiles = @()
        Push-Location $RepoRoot
        try {
            $changed = git diff --name-only $BaseRef 2>$null
            $staged = git diff --cached --name-only 2>$null
            $untracked = git ls-files --others --exclude-standard 2>$null
            $allRel = @($changed) + @($staged) + @($untracked) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique
            foreach ($rel in $allRel) {
                $full = Join-Path $RepoRoot $rel
                if ((Test-Path -LiteralPath $full -PathType Leaf) -and ($full -notmatch $excludedDirRegex) -and ($full -notmatch $excludedFileRegex)) {
                    $diffFiles += Get-Item -LiteralPath $full
                }
            }
        }
        finally {
            Pop-Location
        }
        return $diffFiles
    }

    $roots = switch ($CurrentScope) {
        'All'               { @($RepoRoot) }
        'Backoffice'        { @(Join-Path $RepoRoot 'Backoffice') }
        'BackofficeDotNet'  {
            @(
                'Backoffice\Guts.Api',
                'Backoffice\Guts.Application',
                'Backoffice\Guts.Bootstrapper',
                'Backoffice\Guts.Common',
                'Backoffice\Guts.Domain',
                'Backoffice\Guts.Infrastructure',
                'Backoffice\Guts.Architecture.Tests',
                'Backoffice\Guts.Web\Program.cs'
            ) | ForEach-Object { Join-Path $RepoRoot $_ }
        }
        'BackofficeAngular' { @(Join-Path $RepoRoot 'Backoffice\Guts.Web') }
        'DotNetClient'      { @(Join-Path $RepoRoot 'DotNetClient') }
        'JavaClients'       { @(Join-Path $RepoRoot 'JavaClients') }
        'DevOps'            {
            @(
                Join-Path $RepoRoot '.github'
                Join-Path $RepoRoot 'Backoffice\deploy'
                Join-Path $RepoRoot 'Backoffice\Guts.Api\Dockerfile'
                Join-Path $RepoRoot 'Backoffice\Guts.Web\ClientApp\Dockerfile'
                Join-Path $RepoRoot 'Backoffice\.dockerignore'
                Join-Path $RepoRoot 'Backoffice\docker-compose.yml'
                Join-Path $RepoRoot 'Backoffice\docker-compose.override.yml'
                Join-Path $RepoRoot 'Backoffice\docker-compose.sonarqube.yml'
            )
        }
    }

    $files = @()
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $item = Get-Item -LiteralPath $root
        if (-not $item.PSIsContainer) {
            $files += $item
            continue
        }
        $files += Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
            $_.FullName -notmatch $excludedDirRegex -and
            $_.Name -notmatch $excludedFileRegex -and
            $_.FullName -notmatch '(\\|/)\.agent(\\|/)skills(\\|/)security-audit(\\|/)'
        }
    }
    return $files | Sort-Object FullName -Unique
}

function Run-PatternChecks {
    param(
        [array]$Files,
        [array]$Rules,
        [string]$CategoryTitle
    )

    Write-Section $CategoryTitle
    $hits = @()

    foreach ($rule in $Rules) {
        $matchingFiles = $Files | Where-Object {
            ($_.Name -match $rule.FilePattern) -and
            (-not $rule.ExcludePathPattern -or $_.FullName -notmatch $rule.ExcludePathPattern)
        }

        if (-not $matchingFiles) { continue }

        $paths = @($matchingFiles | ForEach-Object { $_.FullName })
        $matches = Select-String -LiteralPath $paths -Pattern $rule.Regex -CaseSensitive:$false -ErrorAction SilentlyContinue
        foreach ($m in $matches) {
            $relPath = $m.Path.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
            $snippet = $m.Line.Trim()
            if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 117) + '...' }
            $hits += [PSCustomObject]@{
                RuleId   = $rule.Id
                Category = $rule.Label
                Location = "${relPath}:$($m.LineNumber)"
                Snippet  = $snippet
            }
        }
    }

    if ($hits.Count -eq 0) {
        Write-Output "No pattern matches found."
    }
    else {
        Write-Output "| Rule | Category | Location | Match Snippet |"
        Write-Output "| :--- | :--- | :--- | :--- |"
        foreach ($h in ($hits | Select-Object -First 150)) {
            $safeSnippet = ($h.Snippet -replace '\|', '\|' -replace '`', "'")
            Write-Output "| $($h.RuleId) | $($h.Category) | ``$($h.Location)`` | ``$safeSnippet`` |"
        }
        if ($hits.Count -gt 150) {
            Write-Output ""
            Write-Output "*Truncated: showing first 150 of $($hits.Count) matches.*"
        }
    }
}

Write-Output "# GUTS Security Pre-Scan Summary"
Write-Output ""
Write-Output "- **Repository Root**: ``$RepoRoot``"
Write-Output "- **Scope**: ``$Scope``"
Write-Output "- **Timestamp**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss K')"
Write-Output "- **Network Checks Skipped**: ``$SkipNetworkChecks``"

# 1. Dependency / SCA Checks
Write-Section "1. Dependency & Supply Chain Audits (SCA)"

if ($SkipNetworkChecks) {
    Write-Output "Skipped online vulnerability database checks (`-SkipNetworkChecks` was set)."
}
else {
    if ($Scope -in @('All', 'Backoffice', 'BackofficeDotNet')) {
        Write-Output "### Backoffice .NET NuGet Packages"
        $out = dotnet list (Join-Path $RepoRoot 'Backoffice\Guts.slnx') package --vulnerable --include-transitive 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Output "**Warning**: ``dotnet list package --vulnerable`` failed (offline or restore needed):"
        }
        Write-Output '```text'
        $out | ForEach-Object { Write-Output $_ }
        Write-Output '```'
    }

    if ($Scope -in @('All', 'DotNetClient')) {
        Write-Output "### DotNetClient NuGet Packages"
        $out = dotnet list (Join-Path $RepoRoot 'DotNetClient\Guts.sln') package --vulnerable --include-transitive 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Output "**Warning**: ``dotnet list package --vulnerable`` failed (offline or restore needed):"
        }
        Write-Output '```text'
        $out | ForEach-Object { Write-Output $_ }
        Write-Output '```'
    }

    if ($Scope -in @('All', 'Backoffice', 'BackofficeAngular')) {
        Write-Output "### Backoffice Angular npm Audit"
        Push-Location (Join-Path $RepoRoot 'Backoffice\Guts.Web\ClientApp')
        try {
            $out = npm audit --audit-level=moderate 2>&1
            Write-Output '```text'
            $out | Select-Object -First 120 | ForEach-Object { Write-Output $_ }
            Write-Output '```'
        }
        finally {
            Pop-Location
        }
    }
}

if ($Scope -in @('All', 'JavaClients')) {
    Write-Output "### JavaClients Gradle Dependencies & Wrapper Inspection"
    $gradleFiles = Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'JavaClients') -Recurse -Include 'build.gradle', 'gradle-wrapper.properties' -ErrorAction SilentlyContinue
    foreach ($gf in $gradleFiles) {
        $rel = $gf.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        Write-Output "#### ``$rel``"
        Write-Output '```text'
        Get-Content -LiteralPath $gf.FullName | Select-String -Pattern 'implementation|api|compile|testImplementation|distributionUrl|distributionSha256Sum|maven|jcenter|http://' | ForEach-Object { Write-Output "$($_.LineNumber): $($_.Line.Trim())" }
        Write-Output '```'
    }
}

# 2. Repository Hygiene Checks
Write-Section "2. Repository Hygiene & Committed Sensitive Artifacts"

Push-Location $RepoRoot
try {
    $trackedSensitive = git ls-files 2>$null | Where-Object {
        $_ -match '(^|/)Log/.*\.log$' -or
        $_ -match '\.sonar(cloud)?-token$' -or
        $_ -match '(^|/)\.env(\.|$)' -or
        $_ -match '\.(pfx|p12|pem|key)$' -or
        $_ -match '_cache\.txt$'
    }
    if ($trackedSensitive) {
        $logCount = ($trackedSensitive | Where-Object { $_ -match '(^|/)Log/.*\.log$' }).Count
        $otherSensitive = $trackedSensitive | Where-Object { $_ -notmatch '(^|/)Log/.*\.log$' }
        if ($logCount -gt 0) {
            Write-Output "- **Tracked Log Files Found**: $logCount files tracked under ``Log/`` (review for PII/token leakage and gitignore)."
        }
        foreach ($item in $otherSensitive) {
            Write-Output "- **Tracked Sensitive File**: ``$item``"
        }
    }
    else {
        Write-Output "No tracked sensitive artifact files detected."
    }
}
finally {
    Pop-Location
}

# 3. Scoped File Collection
$scopedFiles = Get-ScopedFiles -CurrentScope $Scope
Write-Output ""
Write-Output "*Scanned $($scopedFiles.Count) files in scope ``$Scope``.*"

# 4. Secrets & Credentials Pattern Scan
$secretRules = @(
    @{ Id = 'SEC-PAT-01'; Label = 'Private Key Material'; FilePattern = '.*'; ExcludePathPattern = '(Tests|src[\\/]test)'; Regex = '-----BEGIN (RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----' },
    @{ Id = 'SEC-PAT-02'; Label = 'Hardcoded JWT Token'; FilePattern = '.*'; ExcludePathPattern = '(\.spec\.ts$|Tests|e2e|src[\\/]test)'; Regex = 'eyJ[A-Za-z0-9_-]{15,}\.eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{10,}' },
    @{ Id = 'SEC-PAT-03'; Label = 'Connection String / Config Password'; FilePattern = '\.(json|yml|yaml|xml|config)$'; ExcludePathPattern = '(Tests|src[\\/]test)'; Regex = '(SA_PASSWORD|Password|Pwd)\s*[:=]\s*["'']?[^;\s"''\$\{]{4,}' },
    @{ Id = 'SEC-PAT-04'; Label = 'Cloud/API Secret Key Pattern'; FilePattern = '.*'; ExcludePathPattern = '(Tests|src[\\/]test)'; Regex = '(AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}|sqp_[A-Za-z0-9]{30,})' }
)
Run-PatternChecks -Files $scopedFiles -Rules $secretRules -CategoryTitle "3. Potential Secrets & Credentials Scan"

# 5. Stack-Specific High-Risk Code Sink & Misconfiguration Scan
$sinkRules = @(
    # .NET / ASP.NET Core
    @{ Id = 'DOTNET-01'; Label = 'Raw SQL Execution'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '\b(FromSqlRaw|ExecuteSqlRaw|ExecuteSqlCommand)\s*\(' },
    @{ Id = 'DOTNET-02'; Label = 'AllowAnonymous Endpoint'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '\[AllowAnonymous\]' },
    @{ Id = 'DOTNET-03'; Label = 'Weak/Legacy Hash or Crypto'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '\b(MD5|SHA1|SHA1Managed|MD5CryptoServiceProvider|DESCryptoServiceProvider|TripleDES)\b' },
    @{ Id = 'DOTNET-04'; Label = 'TLS / HTTPS Bypass'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '(RequireHttpsMetadata\s*=\s*false|ServerCertificateCustomValidationCallback|DangerousAcceptAnyServerCertificateValidator)' },
    @{ Id = 'DOTNET-05'; Label = 'Unsafe Deserialization'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '(BinaryFormatter|TypeNameHandling\s*=\s*TypeNameHandling\.(All|Auto|Objects|Arrays))' },
    @{ Id = 'DOTNET-06'; Label = 'Permissive Cookie / CORS'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '(SameSiteMode\.None|AllowAnyOrigin\s*\()' },
    @{ Id = 'DOTNET-07'; Label = 'Security Warning Suppression'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '(SuppressMessage\("Security"|#pragma\s+warning\s+disable\s+S\d+)' },
    @{ Id = 'DOTNET-08'; Label = 'Unconditional Swagger / MCP'; FilePattern = '\.cs$'; ExcludePathPattern = 'Tests'; Regex = '\b(UseSwaggerUi|UseOpenApi|MapMcp|MapQuartzDashboard)\s*\(' },

    # Angular / TypeScript / HTML
    @{ Id = 'NG-01'; Label = 'Angular Sanitizer Bypass'; FilePattern = '\.(ts|html)$'; ExcludePathPattern = '(\.spec\.ts$|e2e)'; Regex = 'bypassSecurityTrust(Html|Script|Style|Url|ResourceUrl)' },
    @{ Id = 'NG-02'; Label = 'Dangerous DOM / Eval Sink'; FilePattern = '\.(ts|html)$'; ExcludePathPattern = '(\.spec\.ts$|e2e)'; Regex = '(\[innerHTML\]|\[outerHTML\]|\.innerHTML\s*=|document\.write\s*\(|\beval\s*\(|new\s+Function\s*\(|enableHtml\s*:\s*true)' },
    @{ Id = 'NG-03'; Label = 'Browser Storage Token Usage'; FilePattern = '\.ts$'; ExcludePathPattern = '(\.spec\.ts$|e2e)'; Regex = '\b(localStorage|sessionStorage)\.(getItem|setItem)' },
    @{ Id = 'NG-04'; Label = 'target=_blank Missing rel'; FilePattern = '\.html$'; ExcludePathPattern = 'e2e'; Regex = 'target="_blank"(?![^>]*rel="[^"]*noopener)' },

    # Java Clients
    @{ Id = 'JAVA-01'; Label = 'Weak Hash Algorithm'; FilePattern = '\.java$'; ExcludePathPattern = 'src[\\/]test'; Regex = 'MessageDigest\.getInstance\s*\(\s*"(MD5|SHA-?1)"' },
    @{ Id = 'JAVA-02'; Label = 'TLS / Hostname Bypass'; FilePattern = '\.java$'; ExcludePathPattern = 'src[\\/]test'; Regex = '(NoopHostnameVerifier|TrustAllStrategy|X509TrustManager|setHostnameVerifier)' },
    @{ Id = 'JAVA-03'; Label = 'Unsafe Deserialization / Exec'; FilePattern = '\.java$'; ExcludePathPattern = 'src[\\/]test'; Regex = '(ObjectInputStream|Runtime\.getRuntime\(\)\.exec|new\s+ProcessBuilder)' },
    @{ Id = 'JAVA-04'; Label = 'Plaintext HTTP or Local Token IO'; FilePattern = '(\.java$|^guts\.json$)'; ExcludePathPattern = 'src[\\/]test'; Regex = '(http://(?!localhost|127\.0\.0\.1|schemas\.)|LOCAL_TOKEN_FILE)' },

    # DevOps / Docker / GitHub Actions
    @{ Id = 'DEVOPS-01'; Label = 'Container Root or :latest Tag'; FilePattern = '(Dockerfile|docker-compose.*\.yml)$'; ExcludePathPattern = ''; Regex = '(^USER\s+root\b|^FROM\s+\S+:latest\b)' },
    @{ Id = 'DEVOPS-02'; Label = 'GitHub Actions Script Injection Risk'; FilePattern = '\.ya?ml$'; ExcludePathPattern = ''; Regex = '\$\{\{\s*github\.event\.(issue|pull_request|comment|head_commit|commits)' },
    @{ Id = 'DEVOPS-03'; Label = 'GitHub Actions Unpinned Tag'; FilePattern = '\.ya?ml$'; ExcludePathPattern = ''; Regex = 'uses:\s+[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v\d+' }
)
Run-PatternChecks -Files $scopedFiles -Rules $sinkRules -CategoryTitle "4. High-Risk Sinks, Misconfigurations & Suppressions"

# 6. Optional External Security Scanners
Write-Section "5. External Security Scanners"
$externalTools = @('trivy', 'gitleaks', 'semgrep', 'osv-scanner')
$installedExternal = @()
foreach ($tool in $externalTools) {
    if (Get-Command $tool -ErrorAction SilentlyContinue) {
        $installedExternal += $tool
    }
}

if ($installedExternal.Count -eq 0) {
    Write-Output 'No optional external scanners (`trivy`, `gitleaks`, `semgrep`, `osv-scanner`) detected on PATH. Native checks and manual checklist audit remain authoritative.'
}
else {
    Write-Output "Installed optional external scanners: ``$($installedExternal -join ', ')``."
    if ($IncludeExternalTools) {
        foreach ($tool in $installedExternal) {
            Write-Output "### Running ``$tool``"
            Write-Output '```text'
            switch ($tool) {
                'gitleaks'    { gitleaks detect --source $RepoRoot --no-git --redact 2>&1 | Select-Object -First 80 }
                'trivy'       { trivy fs --severity HIGH,CRITICAL --scanners vuln,misconfig $RepoRoot 2>&1 | Select-Object -First 100 }
                'osv-scanner' { osv-scanner --recursive $RepoRoot 2>&1 | Select-Object -First 100 }
                'semgrep'     { semgrep --config auto --severity ERROR --quiet $RepoRoot 2>&1 | Select-Object -First 100 }
            }
            Write-Output '```'
        }
    }
    else {
        Write-Output "Re-run with ``-IncludeExternalTools`` to execute them."
    }
}
