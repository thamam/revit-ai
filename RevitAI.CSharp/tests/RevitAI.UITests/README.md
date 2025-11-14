# RevitAI.UITests - Selenium UI Automation Tests

**Automated UI tests for RevitAI using Selenium WebDriver and Windows Application Driver**

---

## Prerequisites

### 1. Windows Application Driver (WinAppDriver)

Download and install from: https://github.com/Microsoft/WinAppDriver/releases

**Installation:**
```bash
# Download WinAppDriver.msi
# Install to default location: C:\Program Files\Windows Application Driver\
```

### 2. Enable Windows Developer Mode

**Steps:**
1. Open Windows Settings
2. Go to: **Privacy & Security** → **For developers**
3. Enable **Developer Mode**
4. Restart your computer

### 3. Revit 2024

- Must be installed at: `C:\Program Files\Autodesk\Revit 2024\Revit.exe`
- If installed elsewhere, update `RevitExecutablePath` in `BaseRevitUITest.cs`

### 4. .NET Framework 4.8

- Should be pre-installed on Windows 10/11
- Verify: Run `dotnet --info` and check for .NET Framework 4.8

---

## Project Structure

```
RevitAI.UITests/
├── RevitAI.UITests.csproj       # Project file with NuGet packages
├── BaseRevitUITest.cs           # Base class for all tests
├── Helpers/
│   └── ElementFinder.cs         # Helper methods for finding UI elements
├── RibbonTests.cs               # Tests for ribbon UI (UC-02)
├── CopilotDialogTests.cs        # Tests for Copilot dialog (UC-03)
├── SettingsDialogTests.cs       # Tests for Settings dialog (UC-04)
├── APIConnectionTests.cs        # Tests for Claude API connection (UC-05)
└── ErrorHandlingTests.cs        # Tests for error scenarios (UC-06, UC-07)
```

---

## Setup

### Step 1: Build the Test Project

```bash
cd RevitAI.CSharp\tests\RevitAI.UITests
dotnet restore
dotnet build --configuration Debug
```

### Step 2: Start WinAppDriver

**Option A: Run Manually**
```powershell
# In PowerShell (as Administrator)
cd "C:\Program Files\Windows Application Driver"
.\WinAppDriver.exe
```

**Leave this running in the background.**

**Option B: Run as Service (Advanced)**
```powershell
# Install as Windows service
sc.exe create WinAppDriver binPath="C:\Program Files\Windows Application Driver\WinAppDriver.exe" start=auto
sc.exe start WinAppDriver
```

---

## Running Tests

### Run All Tests

```bash
cd RevitAI.CSharp\tests\RevitAI.UITests
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~RibbonTests"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~Test_UC02_RibbonDisplaysCorrectly"
```

### Run with Results File

```bash
dotnet test --logger "trx;LogFileName=../../test-results/week1/run-001.trx"
```

---

## Test Execution Flow

Each test follows this pattern:

```
1. [SetUp] Launch Revit (90 second timeout)
2. [SetUp] Connect to WinAppDriver
3. [SetUp] Find Revit main window
4. [Test] Execute test steps
5. [TearDown] Close any open dialogs
6. [TearDown] Close Revit
7. [TearDown] Dispose WinAppDriver session
```

**⚠️ Important:** Tests launch and close Revit for EACH test. This means:
- Tests are slower (2-3 minutes per test including Revit startup)
- Tests are isolated (no state leakage between tests)
- Tests are more reliable (clean slate each time)

---

## Configuration

### Timeout Configuration

Edit `BaseRevitUITest.cs` to adjust timeouts:

```csharp
protected virtual int RevitStartupTimeoutSeconds => 90;  // Default: 90 seconds
protected virtual int ElementWaitTimeoutSeconds => 30;   // Default: 30 seconds
```

### Revit Path Configuration

If Revit is installed in a non-standard location:

```csharp
protected virtual string RevitExecutablePath => @"C:\Your\Custom\Path\Revit.exe";
```

### WinAppDriver URL

If WinAppDriver is running on a different port:

```csharp
protected virtual string WinAppDriverUrl => "http://127.0.0.1:4723";
```

---

## Troubleshooting

### Issue: "Failed to connect to WinAppDriver"

**Cause:** WinAppDriver is not running

**Solution:**
```powershell
cd "C:\Program Files\Windows Application Driver"
.\WinAppDriver.exe
```

### Issue: "Revit executable not found"

**Cause:** Revit is not installed or path is incorrect

**Solution:**
1. Verify Revit is installed
2. Update `RevitExecutablePath` in `BaseRevitUITest.cs`

### Issue: "Revit did not start within 90 seconds"

**Cause:** Machine is slow or Revit takes longer to load

**Solution:**
Increase timeout in `BaseRevitUITest.cs`:
```csharp
protected virtual int RevitStartupTimeoutSeconds => 120; // Increase to 120 seconds
```

### Issue: "Developer Mode is not enabled"

**Cause:** Windows Developer Mode is required for WinAppDriver

**Solution:**
1. Settings → Privacy & Security → For developers
2. Enable Developer Mode
3. Restart computer

### Issue: Tests fail with "Element not found"

**Cause:** UI element locators may be incorrect or timing issue

**Solution:**
1. Check console output for available element names
2. Increase `ElementWaitTimeoutSeconds` if elements load slowly
3. Use `ElementFinder` helper which provides better error messages

### Issue: Revit doesn't close after test

**Cause:** Revit crashed or teardown failed

**Solution:**
Manually kill Revit process:
```powershell
taskkill /F /IM Revit.exe
```

---

## Best Practices

### 1. Use ElementFinder Helpers

```csharp
// Good - provides better error messages
using RevitAI.UITests.Helpers;
var button = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 30);

// Less ideal - generic error messages
var button = driver.FindElementByName("Copilot");
```

### 2. Use LogStep for Debugging

```csharp
[Test]
public void MyTest()
{
    LogStep("Clicking RevitAI tab");
    // ... test code ...

    LogStep("Verifying panel appears");
    // ... test code ...
}
```

### 3. Wait for Elements

```csharp
// Always wait for elements to be ready
var element = WaitForElement(() => driver.FindElementByName("Close"), timeoutSeconds: 10);
```

### 4. Handle Flaky UI

```csharp
// Use retry logic for unreliable clicks
ElementFinder.ClickWithRetry(button, maxAttempts: 3);
```

---

## CI/CD Integration (Future)

When ready for CI/CD:

```yaml
# Example GitHub Actions workflow
- name: Start WinAppDriver
  run: |
    Start-Process "C:\Program Files\Windows Application Driver\WinAppDriver.exe"

- name: Run Tests
  run: dotnet test --logger trx

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/*.trx
```

---

## Current Test Coverage (Week 1)

| Test Class | Use Cases | Status |
|------------|-----------|--------|
| RibbonTests.cs | UC-02: Ribbon UI displays | ✅ Implemented |
| CopilotDialogTests.cs | UC-03: Copilot dialog opens | ✅ Implemented |
| SettingsDialogTests.cs | UC-04: Settings dialog opens | ✅ Implemented |
| APIConnectionTests.cs | UC-05: Test Claude API | ✅ Implemented |
| ErrorHandlingTests.cs | UC-06, UC-07: Error handling | ✅ Implemented |

**Total:** 7 use cases, 6+ test methods

---

## Support

For issues or questions:
- Review `TESTING_EVALUATION.md` in repository root
- Check test console output for detailed error messages
- Review Revit Journal files: `%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\`

---

**Document Version:** 1.0
**Last Updated:** 2025-11-12
**Week 1 Testing POC**
