# SSU: Share the local equivalents of the build, test, YAML, and RSI workflows.
[CmdletBinding()]
param(
    [ValidateSet('Build', 'Yaml', 'Tests', 'Rsis', 'All')]
    [string] $Check = 'All',
    [switch] $NoPause,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$resultsDirectory = Join-Path $repoRoot "bin\local-checks\$runId-$Check"
$transcriptStarted = $false
$workflowLock = $null
$originalGcServer = $env:DOTNET_gcServer
$exitCode = 1

function Invoke-WorkflowCommand {
    param(
        [string] $Command,
        [string[]] $Arguments
    )

    $displayArguments = ($Arguments | ForEach-Object { '"{0}"' -f $_ }) -join ' '
    Write-Host "`n> $Command $displayArguments" -ForegroundColor Cyan
    if ($DryRun) {
        return
    }

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE. See the output above."
    }
}

function Build-DebugOpt {
    Invoke-WorkflowCommand 'dotnet' @(
        'build', 'SpaceStation14.slnx', '--configuration', 'DebugOpt', '--no-restore', '/m'
    )
}

function Test-Content {
    $unitResults = Join-Path $resultsDirectory 'unit'
    $integrationResults = Join-Path $resultsDirectory 'integration'
    Invoke-WorkflowCommand 'dotnet' @(
        'test', '--no-build', '--no-restore', '--configuration', 'DebugOpt',
        'Content.Tests/Content.Tests.csproj', '--',
        'NUnit.ConsoleOut=0', 'NUnit.TestOutputXml=logs', "NUnit.WorkDirectory=$unitResults"
    )

    $env:DOTNET_gcServer = '1'
    Invoke-WorkflowCommand 'dotnet' @(
        'test', '--no-build', '--no-restore', '--configuration', 'DebugOpt',
        'Content.IntegrationTests/Content.IntegrationTests.csproj', '--',
        'NUnit.ConsoleOut=0', 'NUnit.MapWarningTo=Failed', 'NUnit.TestOutputXml=logs',
        "NUnit.WorkDirectory=$integrationResults"
    )
}

function Test-Yaml {
    Invoke-WorkflowCommand 'dotnet' @(
        'build', 'SpaceStation14.slnx', '--configuration', 'Release', '--no-restore',
        '/p:WarningsAsErrors=', '/m'
    )
    Invoke-WorkflowCommand 'dotnet' @(
        'run', '--project', 'Content.YAMLLinter/Content.YAMLLinter.csproj',
        '--configuration', 'Release', '--no-build', '--no-restore'
    )
}

function Test-Rsis {
    if (-not $DryRun) {
        Write-Host 'RSI validation requires Python 3.10+ with Pillow and jsonschema.'
        Write-Host 'Install missing packages with: python -m pip install pillow jsonschema'
    }
    Invoke-WorkflowCommand 'python' @('-c', 'import PIL, jsonschema')
    Invoke-WorkflowCommand 'python' @('RobustToolbox/Schemas/validate_rsis.py', 'Resources/')
}

Push-Location -LiteralPath $repoRoot
try {
    if (-not $DryRun) {
        $lockDirectory = Join-Path $repoRoot 'bin\local-checks'
        New-Item -ItemType Directory -Path $lockDirectory -Force | Out-Null
        try {
            $workflowLock = [IO.File]::Open(
                (Join-Path $lockDirectory 'workflow-checks.lock'),
                [IO.FileMode]::OpenOrCreate,
                [IO.FileAccess]::ReadWrite,
                [IO.FileShare]::None
            )
        }
        # catch [IO.IOException] {
        catch {
            if ($_.Exception -is [IO.IOException] -or $_.Exception.InnerException -is [IO.IOException]) {
                throw 'Another local workflow check is already running. Wait for it to finish before starting another shortcut.'
            }
            throw
        }

        if (-not (Test-Path -LiteralPath 'RobustToolbox/Robust.Shared/Robust.Shared.csproj')) {
            throw 'Engine submodule is missing. Run git submodule update --init --recursive first.'
        }

        [string[]] $requiredCommands = if ($Check -eq 'Rsis') { @('python') } else { @('dotnet') }
        if ($Check -eq 'All') {
            $requiredCommands += 'python'
        }
        foreach ($command in $requiredCommands) {
            if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
                throw "Required command '$command' is missing from PATH."
            }
        }

        New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
        Start-Transcript -Path (Join-Path $resultsDirectory 'console.log') | Out-Null
        $transcriptStarted = $true
    }

    Write-Host "SSU local workflow checks: $Check" -ForegroundColor Cyan
    Write-Host "Repository: $repoRoot"
    if ($DryRun) {
        Write-Host 'Preview only: no builds, restores, tests, or validators will run.'
    }

    if ($Check -ne 'Rsis') {
        Invoke-WorkflowCommand 'dotnet' @('restore', 'SpaceStation14.slnx')
    }

    switch ($Check) {
        'Build' { Build-DebugOpt }
        'Yaml' { Test-Yaml }
        'Tests' {
            Build-DebugOpt
            Test-Content
        }
        'Rsis' { Test-Rsis }
        'All' {
            Build-DebugOpt
            Test-Content
            Test-Yaml
            Test-Rsis
        }
    }

    $exitCode = 0
    if ($DryRun) {
        Write-Host "`n$Check command preview completed." -ForegroundColor Green
    }
    else {
        Write-Host "`n$Check checks completed successfully." -ForegroundColor Green
    }
}
catch {
    Write-Host "`n$Check checks FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $env:DOTNET_gcServer = $originalGcServer
    if ($transcriptStarted) {
        Stop-Transcript | Out-Null
        Write-Host "Results: $resultsDirectory"
    }
    if ($null -ne $workflowLock) {
        $workflowLock.Dispose()
    }
    Pop-Location
}

if (-not $NoPause) {
    [void](Read-Host 'Press Enter to close')
}
exit $exitCode
