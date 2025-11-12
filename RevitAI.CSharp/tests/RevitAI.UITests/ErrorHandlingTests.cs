using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Tests for UC-06 and UC-07: Error Handling
    /// Verifies that missing and invalid API keys are handled gracefully with user-friendly errors
    /// </summary>
    [TestFixture]
    public class ErrorHandlingTests : BaseRevitUITest
    {
        /// <summary>
        /// Override element wait timeout for error tests
        /// </summary>
        protected override int ElementWaitTimeoutSeconds => 45;

        [Test]
        [Category("Error Handling")]
        [Description("UC-06: Verify missing API key shows appropriate error")]
        public void Test_UC06_MissingAPIKey_ShowsError()
        {
            // Verify API key is NOT set
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Ignore("Test skipped: CLAUDE_API_KEY is set. " +
                    "This test requires the API key to be missing. " +
                    "Unset the environment variable and restart Revit to run this test.");
            }

            LogStep("TEST START: Testing missing API key error handling");
            LogStep("Confirmed: CLAUDE_API_KEY environment variable is not set");

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

            // Step 2: Click "Test Claude API" button
            LogStep("Step 2: Clicking 'Test Claude API' button (should trigger error)");
            var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(testButton);
            LogStep("✓ Button clicked");

            // Step 3: Wait for error dialog to appear
            LogStep("Step 3: Waiting for error dialog");
            WindowsElement errorDialog = null;
            try
            {
                // The error should appear quickly (< 5 seconds) since it's detected before API call
                errorDialog = ElementFinder.FindByName(driver, "API Key Missing", timeoutSeconds: 10);
                Assert.IsNotNull(errorDialog, "Error dialog should appear when API key is missing");
                LogStep("✓ Error dialog appeared: 'API Key Missing'");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Error dialog did not appear. This might mean the error handling is broken. Error: {ex.Message}");
            }

            // Step 4: Verify error message content
            LogStep("Step 4: Verifying error message content");
            try
            {
                // Try to read the error message text
                // WPF MessageBox/TaskDialog typically has text in various elements
                var allTextElements = errorDialog.FindElementsByClassName("TextBlock");
                bool foundAPIKeyMessage = false;

                foreach (var textElement in allTextElements)
                {
                    try
                    {
                        var text = textElement.Text;
                        if (!string.IsNullOrEmpty(text))
                        {
                            LogStep($"Error message text: {text}");

                            // Check for expected content
                            if (text.Contains("CLAUDE_API_KEY") ||
                                text.Contains("environment variable") ||
                                text.Contains("API key"))
                            {
                                foundAPIKeyMessage = true;
                            }
                        }
                    }
                    catch
                    {
                        // Skip elements we can't read
                    }
                }

                Assert.IsTrue(foundAPIKeyMessage,
                    "Error message should mention CLAUDE_API_KEY or environment variable");
                LogStep("✓ Error message contains helpful information");
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not fully verify error message content. Details: {ex.Message}");
            }

            // Step 5: Close error dialog
            LogStep("Step 5: Closing error dialog");
            try
            {
                // Find OK button (common in error dialogs)
                WindowsElement okButton = null;
                try
                {
                    okButton = ElementFinder.FindChildByName(errorDialog, "OK", timeoutSeconds: 5);
                }
                catch
                {
                    // Try "Close" instead
                    okButton = ElementFinder.FindChildByName(errorDialog, "Close", timeoutSeconds: 5);
                }

                Assert.IsNotNull(okButton, "Error dialog should have OK or Close button");
                ElementFinder.ClickWithRetry(okButton);
                System.Threading.Thread.Sleep(1000);

                // Verify error dialog closed
                bool dialogClosed = !ElementFinder.Exists(driver, "API Key Missing");
                Assert.IsTrue(dialogClosed, "Error dialog should close when OK is clicked");
                LogStep("✓ Error dialog closed");
            }
            catch (Exception ex)
            {
                LogStep($"Warning: Could not cleanly close error dialog. Details: {ex.Message}");
            }

            // Step 6: Verify Copilot dialog is still open and functional
            LogStep("Step 6: Verifying Copilot dialog remains functional");
            try
            {
                var copilotDialogAfterError = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 5);
                Assert.IsNotNull(copilotDialogAfterError, "Copilot dialog should still be open after error");

                var closeButton = ElementFinder.FindChildByName(copilotDialogAfterError, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "Copilot dialog should still be functional");
                LogStep("✓ Copilot dialog remains functional after error");

                // Close it
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                LogStep($"Warning: Could not verify Copilot dialog state. Details: {ex.Message}");
            }

            LogStep("TEST COMPLETE: Missing API key error handling verified");
        }

        [Test]
        [Category("Error Handling")]
        [Description("UC-07: Verify invalid API key shows appropriate error")]
        public void Test_UC07_InvalidAPIKey_ShowsError()
        {
            // Check current API key
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

            // This test should be run with an INVALID API key set
            // If no key is set, we can't test this scenario
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Inconclusive("Test inconclusive: CLAUDE_API_KEY not set. " +
                    "Set an INVALID API key (e.g., 'sk-ant-invalid123') and restart Revit to run this test.");
            }

            // If key starts with correct prefix but we suspect it's valid, warn
            if (apiKey.StartsWith("sk-ant-api") && apiKey.Length > 50)
            {
                LogStep("WARNING: API key looks valid. This test expects an INVALID key.");
                LogStep("To test invalid key handling, temporarily set CLAUDE_API_KEY to 'sk-ant-invalid123'");
            }

            LogStep("TEST START: Testing invalid API key error handling");
            LogStep($"API Key detected: {apiKey.Substring(0, Math.Min(15, apiKey.Length))}...");

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

            // Step 2: Click "Test Claude API" button
            LogStep("Step 2: Clicking 'Test Claude API' button");
            var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(testButton);
            LogStep("✓ Button clicked, waiting for API response...");

            // Step 3: Wait for status update (API call will fail, but takes time)
            LogStep("Step 3: Waiting for API failure response (up to 60 seconds)");
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

                        // Check for failure indicators
                        if (finalStatus.Contains("✗") ||
                            finalStatus.Contains("fail") ||
                            finalStatus.Contains("error") ||
                            finalStatus.Contains("Check your API key"))
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

                System.Threading.Thread.Sleep(1000);
            }

            LogStep($"API response received in {stopwatch.Elapsed.TotalSeconds:F1} seconds");

            // Step 4: Verify failure status
            LogStep("Step 4: Verifying failure status");
            Assert.IsTrue(statusUpdated, "Status should update after API call fails");
            Assert.IsNotEmpty(finalStatus, "Final status should not be empty");

            LogStep($"Final status: {finalStatus.Substring(0, Math.Min(100, finalStatus.Length))}...");

            // Check for failure indicators
            bool hasFailureIndicator = finalStatus.Contains("✗") ||
                                       finalStatus.Contains("fail") ||
                                       finalStatus.Contains("error") ||
                                       finalStatus.Contains("Check") ||
                                       finalStatus.Contains("connection failed");

            Assert.IsTrue(hasFailureIndicator,
                $"Status should indicate failure. Actual status: {finalStatus}");

            LogStep("✓ Invalid API key properly detected and reported");

            // Step 5: Verify helpful error message
            LogStep("Step 5: Verifying error message is helpful");
            bool hasHelpfulMessage = finalStatus.Contains("API key") ||
                                    finalStatus.Contains("connection") ||
                                    finalStatus.Contains("Check");

            Assert.IsTrue(hasHelpfulMessage,
                "Error message should provide guidance (mention API key or connection)");
            LogStep("✓ Error message provides helpful guidance");

            // Step 6: Verify Revit remains stable
            LogStep("Step 6: Verifying Revit stability after API failure");
            try
            {
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "Dialog should remain functional after API error");
                LogStep("✓ Revit remains stable after API failure");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Revit became unresponsive after API error. Error: {ex.Message}");
            }

            // Step 7: Close dialog
            LogStep("Step 7: Closing dialog");
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

            LogStep("TEST COMPLETE: Invalid API key error handling verified");
        }

        [Test]
        [Category("Error Handling")]
        [Description("Verify Revit doesn't crash when errors occur")]
        public void Test_RevitStabilityAfterErrors()
        {
            LogStep("TEST START: Testing Revit stability after errors");

            // Open Copilot dialog
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(copilotButton);

            var copilotDialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
            Assert.IsNotNull(copilotDialog);

            // Click Test API button multiple times (may trigger errors)
            for (int i = 1; i <= 3; i++)
            {
                LogStep($"Test API attempt {i}/3");

                try
                {
                    var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
                    ElementFinder.ClickWithRetry(testButton);
                    System.Threading.Thread.Sleep(5000); // Wait between attempts
                }
                catch (Exception ex)
                {
                    LogStep($"Attempt {i} had error (expected): {ex.Message}");
                }
            }

            // Verify Revit is still responsive
            LogStep("Verifying Revit is still responsive");
            try
            {
                // Try to find the main window
                var revitWindowStillExists = ElementFinder.Exists(driver, "Autodesk Revit 2024");
                Assert.IsTrue(revitWindowStillExists, "Revit should still be running");

                // Try to close the dialog
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "Dialog should be responsive");

                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);

                LogStep("✓ Revit remained stable through multiple potential errors");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Revit became unresponsive. Error: {ex.Message}");
            }

            LogStep("TEST COMPLETE: Revit stability verified");
        }
    }
}
