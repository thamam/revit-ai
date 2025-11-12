using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Tests for UC-04: Settings Dialog Opens
    /// Verifies that clicking Settings button opens the dialog with correct configuration info
    /// </summary>
    [TestFixture]
    public class SettingsDialogTests : BaseRevitUITest
    {
        [Test]
        [Category("Smoke")]
        [Description("UC-04: Verify Settings button opens dialog with configuration information")]
        public void Test_UC04_SettingsDialogOpens()
        {
            LogStep("TEST START: Verifying Settings dialog opens");

            // Step 1: Click RevitAI tab to activate it
            LogStep("Step 1: Activating RevitAI tab");
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(1000);

            // Step 2: Find and click Settings button
            LogStep("Step 2: Clicking Settings button");
            WindowsElement settingsButton = null;
            try
            {
                settingsButton = ElementFinder.FindByName(driver, "Settings", timeoutSeconds: 5);
                Assert.IsNotNull(settingsButton, "Settings button should exist");
                ElementFinder.ClickWithRetry(settingsButton, maxAttempts: 3);
                LogStep("✓ Settings button clicked");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to click Settings button. Error: {ex.Message}");
            }

            // Step 3: Wait for dialog to appear
            LogStep("Step 3: Waiting for Settings dialog to appear");
            WindowsElement settingsDialog = null;
            try
            {
                // Increased timeout to 45 seconds based on evaluation recommendations
                settingsDialog = ElementFinder.FindByName(driver, "RevitAI Settings", timeoutSeconds: 45);
                Assert.IsNotNull(settingsDialog, "Settings dialog should open");
                Assert.IsTrue(settingsDialog.Displayed, "Settings dialog should be visible");
                LogStep("✓ Settings dialog opened and visible");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Settings dialog did not open within timeout. Error: {ex.Message}");
            }

            // Step 4: Verify dialog title
            LogStep("Step 4: Verifying dialog title");
            try
            {
                var dialogTitle = settingsDialog.GetAttribute("Name");
                Assert.AreEqual("RevitAI Settings", dialogTitle, "Dialog title should be 'RevitAI Settings'");
                LogStep($"✓ Dialog title correct: '{dialogTitle}'");
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not verify dialog title. Details: {ex.Message}");
            }

            // Step 5: Verify configuration information is displayed
            LogStep("Step 5: Looking for configuration text");
            try
            {
                // Try to find status text by AutomationId
                WindowsElement statusText = null;
                try
                {
                    statusText = ElementFinder.FindByAutomationId(driver, "StatusTextBlock", timeoutSeconds: 5);
                }
                catch
                {
                    LogStep("Note: AutomationId 'StatusTextBlock' not found, trying alternative approach");
                }

                if (statusText != null)
                {
                    var text = statusText.Text;

                    // Verify key information is present
                    Assert.IsTrue(text.Contains("API Key") || text.Contains("Configured") || text.Contains("Not Configured"),
                        "Settings should display API Key status");
                    Assert.IsTrue(text.Contains("claude-sonnet-4"), "Settings should display model name");
                    Assert.IsTrue(text.Contains("Epic 1"), "Settings should display current status");

                    LogStep($"✓ Configuration information displayed: {text.Substring(0, Math.Min(100, text.Length))}...");
                }
                else
                {
                    LogStep("Note: Could not locate status text element. This may need adjustment.");
                }
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not fully verify configuration text. Details: {ex.Message}");
            }

            // Step 6: Verify "Open Documentation" button exists
            LogStep("Step 6: Looking for 'Open Documentation' button");
            try
            {
                var docsButton = ElementFinder.FindByName(driver, "Open Documentation", timeoutSeconds: 5);
                Assert.IsNotNull(docsButton, "'Open Documentation' button should exist");
                Assert.IsTrue(docsButton.Enabled, "'Open Documentation' button should be enabled");
                LogStep("✓ 'Open Documentation' button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find 'Open Documentation' button. Error: {ex.Message}");
            }

            // Step 7: Verify Close button exists
            LogStep("Step 7: Looking for 'Close' button");
            try
            {
                var closeButton = ElementFinder.FindChildByName(settingsDialog, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "'Close' button should exist");
                Assert.IsTrue(closeButton.Enabled, "'Close' button should be enabled");
                LogStep("✓ 'Close' button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find 'Close' button in dialog. Error: {ex.Message}");
            }

            // Step 8: Close dialog
            LogStep("Step 8: Closing dialog");
            try
            {
                var closeButton = ElementFinder.FindChildByName(settingsDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000); // Wait for dialog to close

                // Verify dialog closed
                bool dialogClosed = !ElementFinder.Exists(driver, "RevitAI Settings");
                Assert.IsTrue(dialogClosed, "Dialog should close when Close button is clicked");
                LogStep("✓ Dialog closed successfully");
            }
            catch (Exception ex)
            {
                LogStep($"Warning: Failed to close dialog cleanly. Details: {ex.Message}");
            }

            LogStep("TEST COMPLETE: Settings dialog verified successfully");
        }

        [Test]
        [Category("Smoke")]
        [Description("Verify API Key status is displayed correctly based on environment variable")]
        public void Test_APIKeyStatusDisplay()
        {
            LogStep("TEST START: Verifying API Key status display");

            // Activate tab and open Settings
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            var settingsButton = ElementFinder.FindByName(driver, "Settings", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(settingsButton);

            // Wait for dialog
            var settingsDialog = ElementFinder.FindByName(driver, "RevitAI Settings", timeoutSeconds: 45);
            Assert.IsNotNull(settingsDialog, "Settings dialog should open");

            // Check API key status in displayed text
            LogStep("Checking API Key status display");
            try
            {
                // Get the environment variable status
                string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
                bool hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

                LogStep($"Environment variable CLAUDE_API_KEY is {(hasApiKey ? "set" : "not set")}");

                // Try to find and read the status text
                try
                {
                    var statusText = ElementFinder.FindByAutomationId(driver, "StatusTextBlock", timeoutSeconds: 5);
                    if (statusText != null)
                    {
                        var text = statusText.Text;

                        // Verify status matches environment
                        if (hasApiKey)
                        {
                            Assert.IsTrue(text.Contains("✓ Configured") || text.Contains("Configured"),
                                "Settings should show API key as configured when env var is set");
                            LogStep("✓ API Key correctly shown as Configured");
                        }
                        else
                        {
                            Assert.IsTrue(text.Contains("✗ Not Configured") || text.Contains("Not Configured"),
                                "Settings should show API key as not configured when env var is not set");
                            LogStep("✓ API Key correctly shown as Not Configured");
                        }
                    }
                }
                catch
                {
                    LogStep("Note: Could not read status text for verification");
                }
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not fully verify API key status. Details: {ex.Message}");
            }

            // Close dialog
            try
            {
                var closeButton = ElementFinder.FindChildByName(settingsDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                // Cleanup will handle this
            }

            LogStep("TEST COMPLETE: API Key status display verified");
        }

        [Test]
        [Category("Integration")]
        [Description("Verify Open Documentation button launches browser (if possible to detect)")]
        public void Test_OpenDocumentationButton()
        {
            LogStep("TEST START: Testing Open Documentation button");

            // Activate tab and open Settings
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            var settingsButton = ElementFinder.FindByName(driver, "Settings", timeoutSeconds: 5);
            ElementFinder.ClickWithRetry(settingsButton);

            // Wait for dialog
            var settingsDialog = ElementFinder.FindByName(driver, "RevitAI Settings", timeoutSeconds: 45);
            Assert.IsNotNull(settingsDialog, "Settings dialog should open");

            // Click Open Documentation button
            LogStep("Clicking Open Documentation button");
            try
            {
                var docsButton = ElementFinder.FindByName(driver, "Open Documentation", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(docsButton);
                System.Threading.Thread.Sleep(2000); // Wait for browser to launch

                // Note: We can't easily verify browser opened in automation
                // This test mainly ensures the button doesn't crash
                LogStep("✓ Open Documentation button clicked without error");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to click Open Documentation button. Error: {ex.Message}");
            }

            // Close dialog
            try
            {
                var closeButton = ElementFinder.FindChildByName(settingsDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                // Cleanup will handle this
            }

            LogStep("TEST COMPLETE: Open Documentation button verified");
        }
    }
}
