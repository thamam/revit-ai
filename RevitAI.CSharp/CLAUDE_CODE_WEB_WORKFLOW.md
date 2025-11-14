# Claude Code Web Offloading Workflow

**How to Execute Testing POC Using Claude Code Web Interface**

---

## Problem Statement

- **You have:** Windows machine with Revit
- **Claude Code Web runs on:** Linux (can't execute Windows/Revit apps)
- **Goal:** Use Claude Code Web to write test code, analyze results, and iterate

**Question:** How do I actually hand off work to Claude Code Web?

---

## The Answer: GitHub as the Communication Bridge

```
┌─────────────────────────────────────────────────────────────────┐
│                    WORKFLOW ARCHITECTURE                         │
└─────────────────────────────────────────────────────────────────┘

Your Windows Machine          GitHub Repository          Claude Code Web
                                                         (claude.ai/code)
┌────────────────┐            ┌──────────────┐          ┌─────────────────┐
│                │            │              │          │                 │
│  1. You open   │            │              │          │                 │
│  claude.ai/code│───────────▶│              │          │                 │
│  in browser    │            │              │          │                 │
│                │            │              │          │                 │
│  2. Give       │            │              │          │  3. Claude      │
│  instructions  │            │              │◀─────────│  clones repo    │
│  + repo link   │            │              │          │  from GitHub    │
│                │            │              │          │                 │
│                │            │  4. Claude   │          │                 │
│                │            │  creates     │◀─────────│  5. Writes      │
│                │            │  test files  │          │  test code      │
│                │            │              │          │  autonomously   │
│                │            │              │          │                 │
│                │            │  6. Claude   │          │                 │
│                │◀───────────│  commits &   │◀─────────│  7. Commits     │
│  8. You pull   │            │  pushes      │          │  to branch      │
│  changes       │            │              │          │                 │
│                │            │              │          │                 │
│  9. You run    │            │              │          │                 │
│  tests on      │            │              │          │                 │
│  Windows       │            │              │          │                 │
│                │            │              │          │                 │
│  10. Copy      │───────────▶│  11. Push    │          │                 │
│  results to    │            │  results     │──────────▶│  12. Claude     │
│  test-results/ │            │  to GitHub   │          │  analyzes       │
│                │            │              │          │  results        │
│                │            │              │          │                 │
│  13. Pull      │◀───────────│  14. Claude  │◀─────────│  15. Commits    │
│  analysis      │            │  pushes      │          │  analysis       │
│                │            │  analysis    │          │                 │
│                │            │              │          │                 │
└────────────────┘            └──────────────┘          └─────────────────┘
```

**Key Insight:** GitHub repository is the shared workspace. Both you and Claude Code Web read/write to it.

---

## Step-by-Step: How to Execute Week 1

### Phase 1: Initial Setup (One-time)

#### Step 1.1: Prepare the Repository (You do this)

```bash
# On your Windows machine
cd C:\revit-ai
git checkout csharp-sdk-implementation
git pull origin csharp-sdk-implementation

# Ensure you're up to date with all documentation
# You should have:
# - TESTING_EVALUATION.md
# - TESTING_DELEGATION_STRATEGY.md
# - CLAUDE_CODE_WEB_WORKFLOW.md (this file)
```

#### Step 1.2: Create a Work Branch

```bash
# Create a branch for Week 1 work
git checkout -b testing-poc-week1

# Push it to GitHub
git push -u origin testing-poc-week1
```

---

### Phase 2: Offload Task to Claude Code Web

#### Step 2.1: Open Claude Code Web

1. Open browser
2. Go to: https://claude.ai/code
3. Start a new chat

#### Step 2.2: Provide Context (Copy-Paste This Template)

```markdown
# Context

I'm working on RevitAI, a Revit add-in project. I need your help implementing
automated tests using Selenium/WinAppDriver.

**Repository:** https://github.com/thamam/revit-ai
**Branch:** testing-poc-week1
**Working Directory:** RevitAI.CSharp/

**Key Documents to Read:**
1. `TESTING_EVALUATION.md` - Evaluation framework with all use cases
2. `TESTING_DELEGATION_STRATEGY.md` - How we're collaborating
3. `CLAUDE_CODE_WEB_WORKFLOW.md` - This workflow

**Project Context:**
- C# Revit add-in using .NET Framework 4.8
- Already implemented: Epic 1 (Foundation & Core Infrastructure)
- Testing approach: Selenium with WinAppDriver for UI automation
- Main add-in code: `RevitAI.CSharp/`
- Tests should go in: `RevitAI.CSharp/tests/RevitAI.UITests/`

---

# Your Task

**Phase:** Week 1, Day 1, Tasks 1.3-1.5

**What to do:**
1. Clone the repository (branch: testing-poc-week1)
2. Read `TESTING_EVALUATION.md` to understand use cases
3. Create `RevitAI.CSharp/tests/RevitAI.UITests/` project structure
4. Add necessary NuGet packages:
   - Selenium.WebDriver
   - Appium.WebDriver
   - NUnit
5. Write `BaseRevitUITest.cs` base class for all UI tests
6. Write `Helpers/WindowsFinder.cs` helper class
7. Write `Helpers/RevitLauncher.cs` helper class

**Constraints:**
- Target framework: net48
- Tests will run on Windows 11 with Revit 2024 installed
- WinAppDriver will be installed and running on host
- I will execute tests; you write the code

**Deliverables:**
- `.csproj` file for test project
- Base test infrastructure (3 files)
- README explaining how to run tests
- Commit and push all changes to `testing-poc-week1` branch

---

# Inspection Point

When you're done:
1. Commit and push your changes
2. Summarize what you created
3. List any assumptions or questions
4. Wait for my review

I will then:
- Pull your changes
- Review the code
- Provide feedback
- Approve or request changes
```

#### Step 2.3: Claude Code Web Works Autonomously

Claude will:
1. Clone the repo from GitHub
2. Read the documentation files
3. Create the test project structure
4. Write the code files
5. Commit and push to `testing-poc-week1` branch
6. Respond with summary + inspection point

**Important:** Claude Code Web can access the repo, read files, write code, commit, and push - all autonomously!

---

### Phase 3: Review Claude's Work

#### Step 3.1: Pull Changes on Your Machine

```bash
# On your Windows machine
git pull origin testing-poc-week1
```

#### Step 3.2: Review the Files

```bash
# See what changed
git log --oneline -5

# Review specific files
code RevitAI.CSharp/tests/RevitAI.UITests/BaseRevitUITest.cs
code RevitAI.CSharp/tests/RevitAI.UITests/RevitAI.UITests.csproj
```

#### Step 3.3: Provide Feedback

Back in Claude Code Web chat, respond with:

**Option A: Approve**
```markdown
✅ Approved

Reviewed all files. Code looks good.

Proceed to next phase: Write the actual test files
(RibbonTests.cs, CopilotDialogTests.cs, etc.)
```

**Option B: Request Changes**
```markdown
⚠️ Changes Needed

Review comments:

1. `BaseRevitUITest.cs` line 42:
   - Current: 60 second timeout
   - Change to: 90 seconds (Revit is slow on my machine)

2. `RevitLauncher.cs` line 78:
   - Add retry logic: if Revit fails to start, try 3 times
   - I've seen Revit need 2-3 attempts sometimes

3. Question: Should we add logging to a file for debugging?

Please make these changes, then I'll review again.
```

#### Step 3.4: Claude Makes Changes

Claude will:
1. Read your feedback
2. Make the requested changes
3. Commit and push again
4. Respond with "Changes applied, ready for re-review"

---

### Phase 4: Iterative Collaboration

You continue this pattern:

```
┌─────────────────────────────────────────────────────────────┐
│                   ITERATION LOOP                             │
└─────────────────────────────────────────────────────────────┘

1. You give Claude a task in Web interface
2. Claude works autonomously (reads repo, writes code, commits)
3. You pull changes and review
4. You provide feedback or approval
5. LOOP back to step 2 if changes needed
6. When approved, you give Claude the next task
```

**Example Iteration:**

```
You → Claude Web: "Write RibbonTests.cs for UC-02"
Claude Web → Writes code → Commits → Pushes → "Done, review please"
You → Pull → Review → "Line 34 needs to use FindElementByAutomationId instead of ByName"
Claude Web → Fixes → Commits → Pushes → "Fixed, re-review please"
You → Pull → Review → "Approved, write CopilotDialogTests.cs next"
Claude Web → Writes → Commits → Pushes → "Done, review please"
[Continue...]
```

---

### Phase 5: Execute Tests (You on Windows)

When all test code is written and approved:

#### Step 5.1: Build the Project

```bash
# On your Windows machine
cd RevitAI.CSharp\tests\RevitAI.UITests
dotnet restore
dotnet build --configuration Debug
```

#### Step 5.2: Start WinAppDriver

```powershell
# In PowerShell (as Administrator)
cd "C:\Program Files\Windows Application Driver"
.\WinAppDriver.exe
```

Leave this running.

#### Step 5.3: Run Tests

```powershell
# In another PowerShell window
cd C:\revit-ai\RevitAI.CSharp\tests\RevitAI.UITests
dotnet test --logger "console;verbosity=detailed" --logger "trx;LogFileName=../../test-results/week1/run-001.trx"
```

**While tests run:**
- Observe Revit launching
- Note any errors or unexpected behavior
- Take screenshots if tests fail
- Copy console output

#### Step 5.4: Collect Results

```bash
# Create results directory if doesn't exist
mkdir -p test-results/week1

# Console output should be in: TestResults/*.trx
# Copy to organized location
cp TestResults/*.trx test-results/week1/run-001.trx

# Save console output
# (copy from PowerShell into run-001-console.txt)

# If there were failures, save screenshots
# (WinAppDriver captures these automatically in some cases)
```

---

### Phase 6: Hand Results Back to Claude Code Web

#### Step 6.1: Commit Results to Repo

```bash
# Add test results
git add test-results/week1/
git commit -m "Test run 001 - initial execution results"
git push origin testing-poc-week1
```

#### Step 6.2: Report to Claude Code Web

Go back to claude.ai/code chat and post:

```markdown
# Test Run Results - Run 001

**Status:** Completed
**Date:** 2025-11-12 15:30
**Duration:** 145 seconds

**Files committed:**
- ✅ `test-results/week1/run-001.trx` (NUnit XML results)
- ✅ `test-results/week1/run-001-console.txt` (console output)
- ✅ `test-results/week1/screenshots/failure-003.png` (Test 3 failure)

**Summary:**
- Total tests: 6
- Passed: 4
- Failed: 2
- Skipped: 0

**Test Results:**
- ✅ Test 1 (UC-02: Ribbon UI displays): PASSED (3.2s)
- ✅ Test 2 (UC-03: Copilot dialog opens): PASSED (5.1s)
- ❌ Test 3 (UC-04: Settings dialog opens): FAILED (30s timeout)
- ❌ Test 4 (UC-05: Test Claude API): FAILED (dependency on Test 3)
- ✅ Test 5 (UC-06: Missing API key error): PASSED (2.8s)
- ✅ Test 6 (UC-07: Invalid API key error): PASSED (12.3s)

**Observations:**
1. Revit took 52 seconds to launch (longer than expected)
2. Test 3 failed because Settings dialog never appeared
   - Found button, clicked it, but dialog didn't show
   - Timeout after 30 seconds
3. Test 4 skipped because it depends on Copilot dialog being open
4. Error handling tests (5, 6) worked perfectly

**Environment:**
- Windows 11 Pro 23H2
- Revit 2024.1
- WinAppDriver 1.2.1
- .NET Framework 4.8

**Questions:**
1. For Test 3 failure - should we increase timeout or is there a bug?
2. Should Test 4 be independent of Test 3?

**Next Steps:**
Please analyze the XML results and provide:
1. Root cause analysis of failures
2. Recommended fixes
3. Whether we should re-run or fix first
```

#### Step 6.3: Claude Analyzes Results

Claude will:
1. Pull the latest code (with your results)
2. Read the XML files
3. Read your console output
4. Analyze failures
5. Generate `test-results/week1/analysis-001.md`
6. Commit and push analysis
7. Respond with findings and recommendations

---

## Real-World Example: Complete Day 1 Flow

### Morning Session (2 hours)

**You (9:00 AM):**
```markdown
[In claude.ai/code]

Hi! I need help with Week 1, Day 1 of my testing POC.

Repository: https://github.com/thamam/revit-ai
Branch: testing-poc-week1

Task: Create test project infrastructure

Please:
1. Read TESTING_EVALUATION.md
2. Create tests/RevitAI.UITests/ project
3. Write BaseRevitUITest.cs
4. Commit and push

I'll review when you're done.
```

**Claude Code Web (9:05 AM):**
```markdown
✅ Task completed!

Created:
1. tests/RevitAI.UITests/RevitAI.UITests.csproj
   - Added NuGet packages: Selenium, Appium, NUnit
   - Target framework: net48

2. BaseRevitUITest.cs (162 lines)
   - SetUp: Launches Revit, starts WinAppDriver connection
   - FindRevitWindow(): Locates main Revit window
   - TearDown(): Closes Revit, cleans up

3. Helpers/WindowsFinder.cs (85 lines)
   - Helper methods for finding UI elements

4. Helpers/RevitLauncher.cs (103 lines)
   - Manages Revit process lifecycle
   - 90 second timeout for launch

5. README.md
   - Instructions for running tests

Committed to: testing-poc-week1
Commit: abc123def "Add test project infrastructure"

⏸️ Inspection Point:
Please review BaseRevitUITest.cs lines 45-60 (Revit launch logic).
Is 90 seconds adequate for your machine?
```

**You (9:30 AM):**
```bash
# Pull and review
git pull origin testing-poc-week1
code RevitAI.CSharp/tests/RevitAI.UITests/BaseRevitUITest.cs

# After reviewing...
```

```markdown
[In claude.ai/code]

✅ Approved!

90 seconds is fine. Code looks good.

Next task: Write RibbonTests.cs for UC-02
```

**Claude Code Web (9:35 AM):**
```markdown
✅ Task completed!

Created:
1. tests/RevitAI.UITests/RibbonTests.cs (95 lines)
   - Test_UC02_RibbonDisplaysCorrectly()
   - Finds "RevitAI" tab
   - Verifies "AI Copilot" panel
   - Checks for 2 buttons: Copilot, Settings

Committed to: testing-poc-week1
Commit: def456ghi "Add ribbon UI tests (UC-02)"

⏸️ Inspection Point:
Please review the test assertions. Are these the right checks?
```

**[Pattern continues...]**

---

### Afternoon Session (2 hours)

**You (2:00 PM):**
```markdown
[In claude.ai/code]

All test code is approved and merged.

I'm going to run the tests now. Will report results back shortly.
```

**[You run tests on Windows...]**

**You (2:45 PM):**
```bash
# Commit results
git add test-results/week1/run-001.*
git commit -m "Test run 001 results"
git push origin testing-poc-week1
```

```markdown
[In claude.ai/code]

Test run complete. Results committed.

Key findings:
- 4 passed, 2 failed
- Settings dialog test failed (timeout)
- API test failed (dependency)

Please analyze the results and recommend fixes.
```

**Claude Code Web (2:50 PM):**
```markdown
✅ Analysis complete!

Generated: test-results/week1/analysis-001.md

**Root Causes:**
1. Settings button click succeeded, but dialog detection failed
   - Likely: AutomationId mismatch
   - Dialog might be using different property for identification

2. API test failed because it assumes Copilot dialog is already open
   - Bad test dependency
   - Should open dialog itself

**Recommended Fixes:**
1. Update SettingsDialogTests.cs:
   - Try FindElementByName instead of ByAutomationId
   - Increase timeout to 45 seconds
   - Add retry logic

2. Update APIConnectionTests.cs:
   - Remove dependency on external state
   - Open Copilot dialog within the test

3. General: Add more detailed logging

**Priority:** Fix issue #1 first (Settings dialog), then #2

Should I implement these fixes?
```

**You (2:55 PM):**
```markdown
Yes, implement fixes #1 and #2.

Skip #3 (logging) for now - we can add later if needed.
```

**Claude Code Web (3:00 PM):**
```markdown
✅ Fixes implemented!

Updated:
1. SettingsDialogTests.cs
   - Changed to FindElementByName
   - Timeout: 45 seconds
   - Added 3-attempt retry

2. APIConnectionTests.cs
   - Now opens Copilot dialog in test setup
   - Independent of other tests

Committed to: testing-poc-week1
Commit: ghi789jkl "Fix Settings dialog detection and API test dependency"

⏸️ Ready for re-test
Please run tests again and report results.
```

---

## Key Advantages of This Workflow

### 1. **Asynchronous Collaboration**
- You don't need to wait for Claude
- Claude doesn't need to wait for you
- GitHub is the buffer

### 2. **Clear Handoffs**
- Each side knows when it's their turn
- Inspection points are explicit
- Results are version controlled

### 3. **Reproducible**
- All work is in Git history
- Can review past iterations
- Can roll back if needed

### 4. **Context Preserved**
- Claude reads from repo each time
- Always has latest code
- Understands project structure

---

## Practical Tips

### Tip 1: Keep Claude Web Chat Open
Don't close the browser tab between sessions. Claude retains context within a single chat thread.

### Tip 2: Use Descriptive Commit Messages
Claude reads commit messages to understand what changed:
```bash
# Good
git commit -m "Fix Settings dialog detection - changed from AutomationId to Name property"

# Bad
git commit -m "fix"
```

### Tip 3: Structure Your Requests
Claude works best with clear, structured instructions:

**Good:**
```markdown
Task: Write CopilotDialogTests.cs

Requirements:
- Test UC-03: Copilot dialog opens
- Use BaseRevitUITest as parent class
- Verify: dialog title, status text, test button exists
- Timeout: 30 seconds for dialog to appear

Deliverable:
- CopilotDialogTests.cs with one test method
- Commit and push when done
```

**Bad:**
```markdown
Write the copilot test
```

### Tip 4: Batch Related Tasks
Instead of asking Claude to write one file at a time:

```markdown
Task: Write all error handling tests

Files to create:
1. ErrorHandlingTests.cs
   - UC-06: Missing API key test
   - UC-07: Invalid API key test

Both tests should:
- Use BaseRevitUITest
- Open Copilot dialog
- Click "Test Claude API"
- Verify error messages

Commit all together when done.
```

### Tip 5: Provide Real Feedback
When tests fail, give Claude the actual errors:

```markdown
Test failed with this error:

OpenQA.Selenium.WebDriverException:
An element could not be located on the page using the given search parameters.
  at WindowsFinder.FindRevitWindow() in C:\revit-ai\...\WindowsFinder.cs:line 45

This means the window search failed.
Maybe the window title is different?

Can you add logging to show what windows are found?
```

---

## Troubleshooting

### Problem: Claude Code Web can't access my private repo

**Solution:** Make the repo public temporarily, or use a personal access token:

1. GitHub Settings → Developer Settings → Personal Access Tokens
2. Generate token with `repo` scope
3. Give Claude the token in the chat (it's private to that session)

### Problem: Changes aren't showing up when I pull

**Solution:**
```bash
git fetch origin testing-poc-week1
git log origin/testing-poc-week1  # See if commits are there
git pull origin testing-poc-week1  # Pull again
```

### Problem: Claude seems to have forgotten context

**Solution:** Remind Claude of key information:
```markdown
Quick context reminder:
- We're on Week 1, Day 3
- Working on testing-poc-week1 branch
- Just completed test fixes
- About to run tests for 2nd time

[Continue with your request...]
```

---

## Summary: The Offloading Pattern

```
1. You → Claude Web: "Here's the task + repo link"
2. Claude → GitHub: Clone, read docs, understand context
3. Claude → GitHub: Write code, commit, push
4. Claude → You: "Done, please review"
5. You → GitHub: Pull changes
6. You → Local: Review code
7. You → Claude Web: "Approved" or "Change X"
   [If changes needed, loop back to step 3]
8. You → Local: Execute tests (Windows-specific)
9. You → GitHub: Commit test results
10. You → Claude Web: "Results committed, analyze please"
11. Claude → GitHub: Pull results
12. Claude → GitHub: Analyze, write report, commit, push
13. Claude → You: "Analysis complete, here's what I found"
14. LOOP back to step 1 for next task
```

**The key:** GitHub is the shared workspace. Both you and Claude read/write to it, but at different times.

---

## Ready to Start?

When you're ready to begin Week 1, Day 1:

1. Create `testing-poc-week1` branch
2. Push to GitHub
3. Open claude.ai/code
4. Copy the "Context + Task" template from Step 2.2 above
5. Paste it into Claude Code Web
6. Claude will do the work and wait for your review

**Let me know when you want to start!**
