# RevitAI UI Tests - Test Execution Script
# This script runs the Selenium UI tests and generates results

param(
    [Parameter(Mandatory=$false)]
    [string]$TestFilter = "",

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "../../test-results/week1",

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " RevitAI UI Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if WinAppDriver is running
Write-Host "[1/5] Checking WinAppDriver..." -ForegroundColor Yellow
$winAppDriverRunning = Get-Process -Name "WinAppDriver" -ErrorAction SilentlyContinue
if (-not $winAppDriverRunning) {
    Write-Host "  WARNING: WinAppDriver is not running!" -ForegroundColor Red
    Write-Host "  Start WinAppDriver.exe before running tests." -ForegroundColor Red
    Write-Host "  Location: C:\Program Files\Windows Application Driver\WinAppDriver.exe" -ForegroundColor Gray
    Write-Host ""
    $response = Read-Host "Do you want to continue anyway? (y/n)"
    if ($response -ne "y") {
        Write-Host "Exiting..." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ✓ WinAppDriver is running (PID: $($winAppDriverRunning.Id))" -ForegroundColor Green
}

# Check if Revit is already running
Write-Host "[2/5] Checking for running Revit instances..." -ForegroundColor Yellow
$revitProcess = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
if ($revitProcess) {
    Write-Host "  WARNING: Revit is already running!" -ForegroundColor Red
    Write-Host "  Tests will launch their own Revit instances." -ForegroundColor Red
    Write-Host "  Running Revit processes:" -ForegroundColor Gray
    foreach ($proc in $revitProcess) {
        Write-Host "    PID: $($proc.Id)" -ForegroundColor Gray
    }
    Write-Host ""
    $response = Read-Host "Close existing Revit instances? (y/n)"
    if ($response -eq "y") {
        Write-Host "  Closing Revit..." -ForegroundColor Yellow
        $revitProcess | ForEach-Object { $_.Kill() }
        Start-Sleep -Seconds 3
        Write-Host "  ✓ Revit closed" -ForegroundColor Green
    }
} else {
    Write-Host "  ✓ No Revit instances running" -ForegroundColor Green
}

# Build the test project
if (-not $SkipBuild) {
    Write-Host "[3/5] Building test project..." -ForegroundColor Yellow
    Push-Location ../RevitAI.UITests

    dotnet build --configuration Debug 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Build failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }

    Write-Host "  ✓ Build successful" -ForegroundColor Green
    Pop-Location
} else {
    Write-Host "[3/5] Skipping build (--SkipBuild specified)" -ForegroundColor Gray
}

# Create output directory
Write-Host "[4/5] Preparing output directory..." -ForegroundColor Yellow
$resolvedOutputDir = Resolve-Path -Path $OutputDir -ErrorAction SilentlyContinue
if (-not $resolvedOutputDir) {
    New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
    $resolvedOutputDir = Resolve-Path -Path $OutputDir
}
Write-Host "  Output: $resolvedOutputDir" -ForegroundColor Gray

# Generate run ID
$runId = (Get-Date -Format "yyyyMMdd-HHmmss")
$trxFile = Join-Path $resolvedOutputDir "run-$runId.trx"
$consoleFile = Join-Path $resolvedOutputDir "run-$runId-console.txt"

# Run the tests
Write-Host "[5/5] Running tests..." -ForegroundColor Yellow
Write-Host "  This will take several minutes (Revit launches for each test)" -ForegroundColor Gray
Write-Host "  Results will be saved to: $trxFile" -ForegroundColor Gray
Write-Host ""

Push-Location ../RevitAI.UITests

# Build the dotnet test command
$testCommand = "dotnet test --no-build --configuration Debug --logger `"console;verbosity=detailed`" --logger `"trx;LogFileName=$trxFile`""

if ($TestFilter) {
    $testCommand += " --filter `"$TestFilter`""
    Write-Host "  Filter: $TestFilter" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Executing: $testCommand" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Execute and capture output
$output = Invoke-Expression $testCommand 2>&1 | Tee-Object -FilePath $consoleFile

Pop-Location

# Analyze results
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Test Execution Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $trxFile) {
    Write-Host "✓ Results saved:" -ForegroundColor Green
    Write-Host "  TRX: $trxFile" -ForegroundColor Gray
    Write-Host "  Console: $consoleFile" -ForegroundColor Gray
    Write-Host ""

    # Parse TRX for summary (basic parsing)
    [xml]$trx = Get-Content $trxFile
    $counters = $trx.TestRun.ResultSummary.Counters

    Write-Host "Test Summary:" -ForegroundColor Cyan
    Write-Host "  Total:    $($counters.total)" -ForegroundColor Gray
    Write-Host "  Passed:   $($counters.passed)" -ForegroundColor Green
    Write-Host "  Failed:   $($counters.failed)" -ForegroundColor $(if ($counters.failed -gt 0) { "Red" } else { "Gray" })
    Write-Host "  Skipped:  $($counters.inconclusive)" -ForegroundColor Yellow
    Write-Host ""

    if ($counters.failed -gt 0) {
        Write-Host "Some tests failed. Review the TRX file for details." -ForegroundColor Red
        exit 1
    } else {
        Write-Host "All tests passed!" -ForegroundColor Green
        exit 0
    }
} else {
    Write-Host "✗ Results file not found: $trxFile" -ForegroundColor Red
    Write-Host "Test execution may have failed." -ForegroundColor Red
    exit 1
}
