using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Tests for UC-05: Test Claude API Connection
    /// Verifies that the "Test Claude API" button correctly tests the API connection
    /// </summary>
    [TestFixture]
    public class APIConnectionTests : BaseRevitUITest
    {
        /// <summary>
        /// Override element wait timeout for API tests (API calls can take 5-10 seconds)
        /// </summary>
        protected override int ElementWaitTimeoutSeconds => 60; // Longer timeout for API responses

        [Test]
        [Category("Integration")]
        [Description("UC-05: Verify Test Claude API button works with valid API key")]
        public void Test_UC05_TestClaudeAPI_WithValidKey()
        {
            // This test requires CLAUDE_API_KEY environment variable to be set
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Ignore("Test skipped: CLAUDE_API_KEY environment variable not set. " +
                    "Set the variable and restart Revit to run this test.");
            }

            LogStep("TEST START: Testing Claude API connection with valid key");
            LogStep($"API Key detected: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");

            // Step 1: Open Copilot dialog
            LogStep("Step 1: Opening Copilot dialog");
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(1000);

            var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(copilotButton);

            var copilotDialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
            Assert.IsNotNull(copilotDialog, "Copilot dialog should open");
            LogStep("✓ Copilot dialog opened");

            // Step 2: Find "Test Claude API" button
            LogStep("Step 2: Finding 'Test Claude API' button");
            WindowsElement testButton = null;
            try
            {
                testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
                Assert.IsNotNull(testButton, "'Test Claude API' button should exist");
                Assert.IsTrue(testButton.Enabled, "'Test Claude API' button should be enabled");
                LogStep("✓ 'Test Claude API' button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find 'Test Claude API' button. Error: {ex.Message}");
            }

            // Step 3: Get initial status text (before clicking)
            LogStep("Step 3: Reading initial status text");
            string initialStatus = "";
            try
            {
                var statusText = ElementFinder.FindByAutomationId(driver, "StatusTextBlock", timeoutSeconds: 5);
                if (statusText != null)
                {
                    initialStatus = statusText.Text;
                    LogStep($"Initial status: {initialStatus.Substring(0, Math.Min(50, initialStatus.Length))}...");
                }
            }
            catch
            {
                LogStep("Note: Could not read initial status text");
            }

            // Step 4: Click "Test Claude API" button
            LogStep("Step 4: Clicking 'Test Claude API' button");
            try
            {
                ElementFinder.ClickWithRetry(testButton, maxAttempts: 3);
                LogStep("✓ Button clicked, waiting for API response...");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to click 'Test Claude API' button. Error: {ex.Message}");
            }

            // Step 5: Wait for status to update (API call can take 5-15 seconds)
            LogStep("Step 5: Waiting for API response (up to 60 seconds)");
            bool statusUpdated = false;
            string finalStatus = "";

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < 60)
            {
                try
                {
                    var statusText = ElementFinder.FindByAutomationId(driver, "StatusTextBlock", timeoutSeconds: 2);
                    if (statusText != null)
                    {
                        finalStatus = statusText.Text;

                        // Check if status has changed from initial
                        if (finalStatus != initialStatus &&
                            (finalStatus.Contains("✓") || finalStatus.Contains("success") ||
                             finalStatus.Contains("✗") || finalStatus.Contains("fail")))
                        {
                            statusUpdated = true;
                            break;
                        }
                    }
                }
                catch
                {
                    // Continue waiting
                }

                System.Threading.Thread.Sleep(1000); // Check every second
            }

            LogStep($"API response received in {stopwatch.Elapsed.TotalSeconds:F1} seconds");

            // Step 6: Verify success status
            LogStep("Step 6: Verifying success status");
            Assert.IsTrue(statusUpdated, "Status should update after clicking Test API button");
            Assert.IsNotEmpty(finalStatus, "Final status should not be empty");

            LogStep($"Final status: {finalStatus.Substring(0, Math.Min(100, finalStatus.Length))}...");

            // Check for success indicators
            bool hasSuccessIndicator = finalStatus.Contains("✓") ||
                                       finalStatus.Contains("success") ||
                                       finalStatus.Contains("successful") ||
                                       finalStatus.Contains("connected");

            Assert.IsTrue(hasSuccessIndicator,
                $"Status should indicate success. Actual status: {finalStatus}");

            LogStep("✓ Claude API connection successful!");

            // Step 7: Verify Revit remains stable
            LogStep("Step 7: Verifying Revit stability");
            try
            {
                // Ensure dialog is still responsive
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "Dialog should still be responsive after API call");
                LogStep("✓ Revit remains stable and responsive");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Revit became unresponsive after API call. Error: {ex.Message}");
            }

            // Step 8: Close dialog
            LogStep("Step 8: Closing dialog");
            try
            {
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);
                LogStep("✓ Dialog closed");
            }
            catch
            {
                // Cleanup will handle this
            }

            LogStep("TEST COMPLETE: Claude API connection test passed");
        }

        [Test]
        [Category("Integration")]
        [Description("Verify API test completes within reasonable time (< 30 seconds)")]
        public void Test_APIResponseTime()
        {
            // Check for API key
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Ignore("Test skipped: CLAUDE_API_KEY not set");
            }

            LogStep("TEST START: Measuring API response time");

            // Open Copilot dialog
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(copilotButton);

            var copilotDialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
            Assert.IsNotNull(copilotDialog);

            // Click Test API and measure time
            var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ElementFinder.ClickWithRetry(testButton);

            // Wait for status update
            bool statusUpdated = false;
            while (stopwatch.Elapsed.TotalSeconds < 60 && !statusUpdated)
            {
                try
                {
                    var statusText = ElementFinder.FindByAutomationId(driver, "StatusTextBlock", timeoutSeconds: 2);
                    if (statusText != null)
                    {
                        var text = statusText.Text;
                        if (text.Contains("✓") || text.Contains("success") || text.Contains("✗") || text.Contains("fail"))
                        {
                            statusUpdated = true;
                            break;
                        }
                    }
                }
                catch { }

                System.Threading.Thread.Sleep(1000);
            }

            stopwatch.Stop();
            double responseTimeSeconds = stopwatch.Elapsed.TotalSeconds;

            LogStep($"API response time: {responseTimeSeconds:F2} seconds");

            // Assert response time is reasonable (< 30 seconds)
            Assert.Less(responseTimeSeconds, 30.0,
                $"API should respond within 30 seconds. Actual: {responseTimeSeconds:F2}s");

            LogStep($"✓ API responded in acceptable time: {responseTimeSeconds:F2}s");

            // Close dialog
            try
            {
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);
            }
            catch { }

            LogStep("TEST COMPLETE: API response time verified");
        }

        [Test]
        [Category("Integration")]
        [Description("Verify Test API button can be clicked multiple times without issues")]
        public void Test_MultipleAPITests()
        {
            // Check for API key
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Ignore("Test skipped: CLAUDE_API_KEY not set");
            }

            LogStep("TEST START: Testing multiple API calls");

            // Open Copilot dialog
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(copilotButton);

            var copilotDialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
            Assert.IsNotNull(copilotDialog);

            // Click Test API button 3 times
            for (int i = 1; i <= 3; i++)
            {
                LogStep($"API test attempt {i}/3");

                var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
                Assert.IsTrue(testButton.Enabled, $"Button should be enabled for attempt {i}");

                ElementFinder.ClickWithRetry(testButton);

                // Wait for response
                System.Threading.Thread.Sleep(10000); // Wait 10 seconds between tests

                LogStep($"✓ Attempt {i} completed");
            }

            // Verify dialog still responsive
            var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
            Assert.IsNotNull(closeButton, "Dialog should still be responsive after 3 API calls");

            ElementFinder.ClickWithRetry(closeButton);
            System.Threading.Thread.Sleep(1000);

            LogStep("TEST COMPLETE: Multiple API calls handled successfully");
        }
    }
}
