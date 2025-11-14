# Week 1 Delivery: Selenium UI Test Infrastructure

**Status**: ✅ **Code Complete - Ready for Execution**
**Branch**: `csharp-sdk-implementation`
**Delivery Date**: 2025-11-12
**Next Step**: Execute tests on Windows machine with Revit 2024

---

## 📦 What Was Delivered

### Test Infrastructure (Days 1-2)

#### **1. Test Project Configuration**
- **File**: `tests/RevitAI.UITests/RevitAI.UITests.csproj`
- **Lines**: 28
- **Contents**: NUnit 3.13.3, Selenium WebDriver 4.15.0, Appium.WebDriver 5.0.0-beta01
- **Purpose**: Complete test project with all required NuGet packages

#### **2. Base Test Class**
- **File**: `tests/RevitAI.UITests/BaseRevitUITest.cs`
- **Lines**: 280
- **Key Features**:
  - Revit lifecycle management (launch/cleanup)
  - WinAppDriver connection handling
  - Configurable timeouts (90s Revit startup, 30s element wait)
  - Structured logging with step tracking
  - Graceful error handling and cleanup
- **Configurable Properties**:
  ```csharp
  protected virtual int RevitStartupTimeoutSeconds => 90;
  protected virtual int ElementWaitTimeoutSeconds => 30;
  protected virtual string RevitExecutablePath => @"C:\Program Files\Autodesk\Revit 2024\Revit.exe";
  protected virtual string WinAppDriverUrl => "http://127.0.0.1:4723";
  ```

#### **3. Helper Utilities**
- **File**: `tests/RevitAI.UITests/Helpers/ElementFinder.cs`
- **Lines**: 160
- **Key Methods**:
  - `FindByName()` - Find elements with diagnostic error messages
  - `FindByAutomationId()` - Find by AutomationId property
  - `FindChildByName()` - Search within parent element
  - `ClickWithRetry()` - Retry logic for flaky UI (max 3 attempts)
  - `Exists()` - Check element presence without throwing
- **Benefits**: Better error messages listing available elements when searches fail

#### **4. Test Class Files** (5 files, 13 tests total)

| Test File | Tests | Lines | Use Cases Covered |
|-----------|-------|-------|-------------------|
| `RibbonTests.cs` | 2 | 140 | UC-02: Ribbon displays correctly |
| `CopilotDialogTests.cs` | 2 | 170 | UC-03: Copilot dialog opens |
| `SettingsDialogTests.cs` | 3 | 210 | UC-04: Settings dialog displays config |
| `APIConnectionTests.cs` | 3 | 250 | UC-05: Test Claude API connection |
| `ErrorHandlingTests.cs` | 3 | 240 | UC-06/UC-07: Error handling (missing/invalid API key) |
| **TOTAL** | **13** | **1,010** | **All Epic 1 use cases** |

#### **5. Execution Scripts**

**`tests/scripts/run-tests.ps1`** (150 lines)
- Checks WinAppDriver is running
- Checks for existing Revit instances
- Builds test project
- Executes tests with TRX logger
- Generates timestamped results: `test-results/week1/run-YYYYMMDD-HHMMSS.trx`
- Parses and displays test summary
- Returns exit code (0 = pass, 1 = fail)

**`tests/scripts/setup-check.ps1`** (125 lines)
- Verifies Windows Developer Mode enabled
- Checks WinAppDriver installation and status
- Checks Revit 2024 installation
- Checks .NET SDK installation
- Checks CLAUDE_API_KEY environment variable
- Attempts test project build
- Provides actionable error messages

#### **6. Documentation**

**`tests/RevitAI.UITests/README.md`** (225 lines)
- Complete setup guide
- Prerequisites checklist
- Build and run instructions
- Configuration options
- Troubleshooting guide
- Common issues and solutions

---

## 🎯 Test Coverage Summary

### Epic 1 Use Cases Mapped to Tests

| Use Case | Functional Requirement | Test Method | Expected Outcome |
|----------|------------------------|-------------|------------------|
| **UC-02** | RevitAI ribbon tab displays with AI Copilot panel and two buttons | `RibbonTests.Test_UC02_RibbonDisplaysCorrectly` | Tab visible, buttons enabled |
| **UC-03** | Clicking Copilot button opens RevitAI Copilot dialog | `CopilotDialogTests.Test_UC03_CopilotDialogOpens` | Dialog opens with Test API button |
| **UC-04** | Clicking Settings button opens configuration dialog | `SettingsDialogTests.Test_UC04_SettingsDialogOpens` | Dialog shows API key status, model info |
| **UC-05** | Test Claude API button sends request and displays status | `APIConnectionTests.Test_UC05_TestClaudeAPI_WithValidKey` | Status updates to "✓ success" within 30s |
| **UC-06** | Missing API key shows user-friendly error | `ErrorHandlingTests.Test_UC06_MissingAPIKey_ShowsError` | Error dialog appears mentioning CLAUDE_API_KEY |
| **UC-07** | Invalid API key shows appropriate error message | `ErrorHandlingTests.Test_UC07_InvalidAPIKey_ShowsError` | Status shows failure with helpful message |

### Additional Test Coverage

Beyond the core use cases, tests also verify:
- **UI Stability**: Revit doesn't crash after errors (3 tests)
- **Performance**: API responses within 30 seconds (1 test)
- **Reliability**: Multiple API calls work correctly (1 test)
- **Configuration Display**: API key status matches environment (1 test)
- **Documentation Access**: Open Documentation button works (1 test)

**Total Coverage**: 13 tests covering 6 use cases + stability/performance

---

## 🚀 How to Execute Tests (Next Steps)

### Prerequisites

Before running tests, ensure:
1. ✅ Windows 10/11 with Developer Mode enabled
2. ✅ WinAppDriver installed: `C:\Program Files\Windows Application Driver\WinAppDriver.exe`
3. ✅ Revit 2024 installed: `C:\Program Files\Autodesk\Revit 2024\Revit.exe`
4. ✅ .NET Framework 4.8 SDK installed
5. ✅ `CLAUDE_API_KEY` environment variable set (optional, some tests will skip)

**Run the setup check script to verify**:
```powershell
cd RevitAI.CSharp/tests/scripts
.\setup-check.ps1
```

### Step-by-Step Execution

#### **Step 1: Start WinAppDriver**
Open PowerShell as Administrator:
```powershell
cd "C:\Program Files\Windows Application Driver"
.\WinAppDriver.exe
```
Leave this window open. You should see:
```
Windows Application Driver listening for requests at: http://127.0.0.1:4723/
```

#### **Step 2: Close Existing Revit Instances**
Close any running Revit instances to avoid conflicts.

#### **Step 3: Run Tests**
Open a new PowerShell window (normal user, not admin):
```powershell
cd RevitAI.CSharp/tests/scripts
.\run-tests.ps1
```

**What will happen**:
- Script checks prerequisites
- Builds test project
- Executes 13 tests
- Each test launches a fresh Revit instance (takes ~2 minutes per test)
- Total runtime: ~25-30 minutes for full suite
- Results saved to `test-results/week1/run-YYYYMMDD-HHMMSS.trx`

#### **Step 4: Run Individual Test Categories** (Optional)
To run specific test categories:

```powershell
# Smoke tests only (UC-02, UC-03, UC-04) - 7 tests
dotnet test --filter "Category=Smoke"

# Integration tests only (UC-05 API tests) - 3 tests
dotnet test --filter "Category=Integration"

# Error handling tests only (UC-06, UC-07) - 3 tests
dotnet test --filter "Category=Error Handling"
```

#### **Step 5: Run Specific Use Case**
To test a specific use case:
```powershell
# UC-05 only
dotnet test --filter "FullyQualifiedName~Test_UC05"

# UC-06 only
dotnet test --filter "FullyQualifiedName~Test_UC06"
```

---

## 📊 Expected Test Results

### Tests That Should Pass (API Key Configured)

If `CLAUDE_API_KEY` environment variable is set with a **valid** key:

| Test | Expected Result | Duration |
|------|----------------|----------|
| UC-02: Ribbon displays | ✅ PASS | ~2 min |
| UC-03: Copilot dialog opens | ✅ PASS | ~2 min |
| UC-04: Settings dialog opens | ✅ PASS | ~2 min |
| UC-05: Test Claude API (valid key) | ✅ PASS | ~2.5 min |
| UC-06: Missing API key error | ⚠️ SKIP (API key is set) | ~0.5 min |
| UC-07: Invalid API key error | ⚠️ SKIP or FAIL* | ~2.5 min |
| Stability tests | ✅ PASS | ~3 min |
| Performance tests | ✅ PASS | ~2.5 min |

*UC-07 expects an INVALID API key. If your key is valid, the test will warn.

### Tests That Should Pass (API Key NOT Configured)

If `CLAUDE_API_KEY` is not set:

| Test | Expected Result | Duration |
|------|----------------|----------|
| UC-02: Ribbon displays | ✅ PASS | ~2 min |
| UC-03: Copilot dialog opens | ✅ PASS | ~2 min |
| UC-04: Settings dialog opens | ✅ PASS | ~2 min |
| UC-05: Test Claude API | ⚠️ SKIP (API key not set) | ~0.5 min |
| UC-06: Missing API key error | ✅ PASS | ~2 min |
| UC-07: Invalid API key error | ⚠️ SKIP (API key not set) | ~0.5 min |

**Optimal test configuration**:
- Run tests WITH valid API key to cover UC-05
- Manually unset key and re-run to cover UC-06
- Set invalid key (`sk-ant-invalid123`) to cover UC-07

---

## 📁 Test Results Location

After execution, find results at:
```
RevitAI.CSharp/test-results/week1/
├── run-20251112-143025.trx          # Test results (XML)
├── run-20251112-143025-console.txt  # Console output
└── ...more runs...
```

### Interpreting TRX Results

The `run-tests.ps1` script automatically parses and displays:
```
Test Summary:
  Total:    13
  Passed:   10
  Failed:   0
  Skipped:  3
```

**Expected first run results**:
- **10 passed**: UC-02, UC-03, UC-04 smoke tests + stability tests
- **3 skipped**: UC-05, UC-06, UC-07 (depending on API key configuration)

---

## 🔍 Troubleshooting

### Common Issues

#### ❌ "WinAppDriver is not running"
**Solution**:
```powershell
cd "C:\Program Files\Windows Application Driver"
.\WinAppDriver.exe
```
Leave the window open during testing.

#### ❌ "Element not found: RevitAI"
**Possible causes**:
1. RevitAI add-in not loaded in Revit
2. Revit startup timeout too short (increase from 90s)

**Solution**:
```csharp
// In BaseRevitUITest.cs, increase timeout:
protected override int RevitStartupTimeoutSeconds => 120; // Was 90
```

#### ❌ "Failed to launch Revit"
**Check**:
- Revit path: `C:\Program Files\Autodesk\Revit 2024\Revit.exe`
- Revit is installed and licensed
- No corrupted Revit install

#### ❌ Tests hang or freeze
**Solution**:
1. Close all Revit instances manually
2. Kill WinAppDriver: `Stop-Process -Name "WinAppDriver"`
3. Restart WinAppDriver
4. Re-run tests

#### ❌ "Could not resolve AutomationId: StatusTextBlock"
**Possible causes**:
- WPF dialog structure changed
- AutomationId not set in XAML

**Investigation**:
- Tests include diagnostic output listing available elements
- Check console output for available element names

---

## 📈 Week 1 Evaluation Metrics

After executing tests, we'll calculate:

### 1. **Coverage Score** (40% weight)
```
Coverage = (Use Cases Covered / Total Use Cases) × 100
Expected: 6/6 = 100%
```

### 2. **Automation Score** (35% weight)
```
Automation = (Automated Steps / Total Steps) × 100
Expected: ~95% (only test execution is manual)
```

### 3. **Reliability Score** (25% weight)
Measured after 10 test runs:
```
Reliability = (Successful Runs / Total Runs) × 100
Target: ≥90% for production readiness
```

### 4. **Pass Rate**
```
Pass Rate = (Passed Tests / Total Tests - Skipped) × 100
Target: ≥90%
```

### 5. **Flakiness Rate**
```
Flakiness = (Tests with inconsistent results / Total Tests) × 100
Target: ≤10%
```

---

## 📋 Week 1 Checklist

- [x] Test infrastructure created (Day 1)
- [x] All test files written (Day 2)
- [x] Execution scripts created (Day 2)
- [ ] **→ Execute tests first time (Day 3 - YOUR TASK)**
- [ ] Analyze results and fix issues (Day 4)
- [ ] Execute 10 reliability runs (Day 5)
- [ ] Calculate evaluation metrics (Day 5)
- [ ] Generate Week 1 report (Day 5)

---

## 🎯 Success Criteria for Week 1

### Minimum Viable (Go/No-Go)
- [ ] At least 8 out of 13 tests pass (>60%)
- [ ] UC-02, UC-03, UC-04 (ribbon and dialogs) all pass
- [ ] No Revit crashes during test execution
- [ ] Test results are reproducible across 3 runs

### Target Goals
- [ ] 10+ out of 13 tests pass (>75%)
- [ ] All smoke tests pass consistently
- [ ] UC-05 (API connection) passes with valid key
- [ ] Reliability ≥80% across 10 runs

### Stretch Goals
- [ ] All 13 tests pass (100%)
- [ ] Reliability ≥95% across 10 runs
- [ ] Average test execution time <2 minutes per test
- [ ] Zero false positives/negatives detected

---

## 🔄 Next Steps After Execution

### If Tests Mostly Pass (≥80%)
1. Commit test results to repo
2. Run reliability testing (10 iterations)
3. Calculate final Week 1 metrics
4. Proceed to Week 2: Windows VM POC

### If Tests Have Issues (<80%)
1. Commit test results with failure details
2. Analyze failure patterns (timeout? element not found? crashes?)
3. Implement fixes
4. Re-run tests
5. Iterate until ≥80% pass rate

### Handoff Protocol
After running tests, create:
```
test-results/week1/RESULTS.md
```

Include:
- Pass/Fail/Skip counts
- Failed test details (error messages, screenshots if possible)
- Any unexpected behavior
- System information (Windows version, Revit version)
- Execution duration

---

## 📞 Support

If you encounter issues during execution:
1. Check `test-results/week1/run-YYYYMMDD-HHMMSS-console.txt` for detailed logs
2. Look for "Available elements:" diagnostic output when elements not found
3. Ensure WinAppDriver console shows no errors
4. Verify Revit launches successfully outside of tests

---

## 📝 Code Statistics

**Total Deliverable**:
- **Lines of code**: 1,810 (tests + infrastructure)
- **Test coverage**: 6 use cases, 13 test methods
- **Files created**: 10 (5 tests + base + helper + project + 2 scripts)
- **Documentation**: 225 lines (README.md)

**Quality Metrics**:
- Configurable timeouts: ✅
- Retry logic for flaky UI: ✅
- Diagnostic error messages: ✅
- Test isolation: ✅
- Cleanup on failure: ✅
- Structured logging: ✅

---

**Status**: ✅ **READY FOR EXECUTION**
**Action Required**: Run tests on Windows machine and report results
