# RevitAI UI Tests - Setup Verification Script
# Checks if all prerequisites are installed and configured correctly

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " RevitAI Test Environment Check" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# Check 1: Windows Developer Mode
Write-Host "[1/6] Checking Windows Developer Mode..." -ForegroundColor Yellow
$devMode = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -ErrorAction SilentlyContinue
if ($devMode -and $devMode.AllowDevelopmentWithoutDevLicense -eq 1) {
    Write-Host "  ✓ Developer Mode is enabled" -ForegroundColor Green
} else {
    Write-Host "  ✗ Developer Mode is NOT enabled" -ForegroundColor Red
    Write-Host "    Enable it: Settings → Privacy & Security → For developers → Developer Mode" -ForegroundColor Gray
    $allGood = $false
}

# Check 2: WinAppDriver
Write-Host "[2/6] Checking WinAppDriver installation..." -ForegroundColor Yellow
$winAppDriverPath = "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
if (Test-Path $winAppDriverPath) {
    Write-Host "  ✓ WinAppDriver is installed" -ForegroundColor Green
    Write-Host "    Location: $winAppDriverPath" -ForegroundColor Gray

    # Check if running
    $winAppDriverProcess = Get-Process -Name "WinAppDriver" -ErrorAction SilentlyContinue
    if ($winAppDriverProcess) {
        Write-Host "  ✓ WinAppDriver is currently running (PID: $($winAppDriverProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ WinAppDriver is NOT running" -ForegroundColor Yellow
        Write-Host "    Start it with: cd `"$winAppDriverPath`"; .\WinAppDriver.exe" -ForegroundColor Gray
    }
} else {
    Write-Host "  ✗ WinAppDriver is NOT installed" -ForegroundColor Red
    Write-Host "    Download from: https://github.com/Microsoft/WinAppDriver/releases" -ForegroundColor Gray
    $allGood = $false
}

# Check 3: Revit 2024
Write-Host "[3/6] Checking Revit 2024 installation..." -ForegroundColor Yellow
$revitPath = "C:\Program Files\Autodesk\Revit 2024\Revit.exe"
if (Test-Path $revitPath) {
    Write-Host "  ✓ Revit 2024 is installed" -ForegroundColor Green
    Write-Host "    Location: $revitPath" -ForegroundColor Gray

    # Check if running
    $revitProcess = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
    if ($revitProcess) {
        Write-Host "  ⚠ Revit is currently running (PID: $($revitProcess.Id))" -ForegroundColor Yellow
        Write-Host "    Tests will launch their own Revit instances. Close existing instances before testing." -ForegroundColor Gray
    }
} else {
    Write-Host "  ✗ Revit 2024 is NOT installed at default location" -ForegroundColor Red
    Write-Host "    If installed elsewhere, update RevitExecutablePath in BaseRevitUITest.cs" -ForegroundColor Gray
    $allGood = $false
}

# Check 4: .NET SDK
Write-Host "[4/6] Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion) {
    Write-Host "  ✓ .NET SDK is installed (version: $dotnetVersion)" -ForegroundColor Green
} else {
    Write-Host "  ✗ .NET SDK is NOT installed" -ForegroundColor Red
    Write-Host "    Download from: https://dotnet.microsoft.com/download" -ForegroundColor Gray
    $allGood = $false
}

# Check 5: Claude API Key
Write-Host "[5/6] Checking Claude API Key..." -ForegroundColor Yellow
$apiKey = [Environment]::GetEnvironmentVariable("CLAUDE_API_KEY", "User")
if (-not $apiKey) {
    $apiKey = [Environment]::GetEnvironmentVariable("CLAUDE_API_KEY", "Machine")
}

if ($apiKey) {
    Write-Host "  ✓ CLAUDE_API_KEY is set" -ForegroundColor Green
    Write-Host "    Value: $($apiKey.Substring(0, [Math]::Min(15, $apiKey.Length)))..." -ForegroundColor Gray

    if ($apiKey.StartsWith("sk-ant-")) {
        Write-Host "  ✓ API key format looks correct" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ API key format looks unusual (expected to start with 'sk-ant-')" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ CLAUDE_API_KEY is NOT set" -ForegroundColor Yellow
    Write-Host "    Some tests will be skipped (UC-05, UC-07)" -ForegroundColor Gray
    Write-Host "    To set: setx CLAUDE_API_KEY `"sk-ant-your-key-here`"" -ForegroundColor Gray
}

# Check 6: Test Project
Write-Host "[6/6] Checking test project..." -ForegroundColor Yellow
$testProject = "../RevitAI.UITests/RevitAI.UITests.csproj"
if (Test-Path $testProject) {
    Write-Host "  ✓ Test project exists" -ForegroundColor Green

    # Try to build
    Write-Host "  Attempting test build..." -ForegroundColor Gray
    Push-Location ../RevitAI.UITests
    $buildOutput = dotnet build --configuration Debug 2>&1
    $buildSuccess = $LASTEXITCODE -eq 0
    Pop-Location

    if ($buildSuccess) {
        Write-Host "  ✓ Test project builds successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Test project build failed" -ForegroundColor Red
        Write-Host "    Run dotnet build in the test project directory for details" -ForegroundColor Gray
        $allGood = $false
    }
} else {
    Write-Host "  ✗ Test project not found at: $testProject" -ForegroundColor Red
    $allGood = $false
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($allGood) {
    Write-Host "✓ All prerequisites are installed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You are ready to run tests." -ForegroundColor Green
    Write-Host "Run: .\run-tests.ps1" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "✗ Some prerequisites are missing or not configured" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install missing components before running tests." -ForegroundColor Yellow
    Write-Host ""
}

# Next steps
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Start WinAppDriver: cd 'C:\Program Files\Windows Application Driver'; .\WinAppDriver.exe" -ForegroundColor Gray
Write-Host "2. Run tests: .\run-tests.ps1" -ForegroundColor Gray
Write-Host "3. Check results: ..\..\..\test-results\week1\" -ForegroundColor Gray
Write-Host ""
