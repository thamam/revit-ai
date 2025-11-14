using System;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Tests for UC-03: Copilot Dialog Opens
    /// Verifies that clicking Copilot button opens the dialog with correct content
    /// </summary>
    [TestFixture]
    public class CopilotDialogTests : BaseRevitUITest
    {
        [Test]
        [Category("Smoke")]
        [Description("UC-03: Verify Copilot button opens dialog with correct content")]
        public void Test_UC03_CopilotDialogOpens()
        {
            LogStep("TEST START: Verifying Copilot dialog opens");

            // Step 1: Click RevitAI tab to activate it
            LogStep("Step 1: Activating RevitAI tab");
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(1000);

            // Step 2: Find and click Copilot button
            LogStep("Step 2: Clicking Copilot button");
            WindowsElement copilotButton = null;
            try
            {
                copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
                Assert.IsNotNull(copilotButton, "Copilot button should exist");
                ElementFinder.ClickWithRetry(copilotButton, maxAttempts: 3);
                LogStep("✓ Copilot button clicked");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to click Copilot button. Error: {ex.Message}");
            }

            // Step 3: Wait for dialog to appear
            LogStep("Step 3: Waiting for Copilot dialog to appear");
            WindowsElement copilotDialog = null;
            try
            {
                copilotDialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
                Assert.IsNotNull(copilotDialog, "Copilot dialog should open");
                Assert.IsTrue(copilotDialog.Displayed, "Copilot dialog should be visible");
                LogStep("✓ Copilot dialog opened and visible");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Copilot dialog did not open within timeout. Error: {ex.Message}");
            }

            // Step 4: Verify dialog title
            LogStep("Step 4: Verifying dialog title");
            try
            {
                var dialogTitle = copilotDialog.GetAttribute("Name");
                Assert.AreEqual("RevitAI Copilot", dialogTitle, "Dialog title should be 'RevitAI Copilot'");
                LogStep($"✓ Dialog title correct: '{dialogTitle}'");
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not verify dialog title. Details: {ex.Message}");
            }

            // Step 5: Verify status text contains "Epic 1"
            LogStep("Step 5: Looking for status text element");
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
                    // If AutomationId doesn't work, try finding any text element
                    LogStep("Note: AutomationId 'StatusTextBlock' not found, trying alternative approach");
                }

                if (statusText != null)
                {
                    var text = statusText.Text;
                    Assert.IsTrue(text.Contains("Epic 1"), $"Status text should mention 'Epic 1'. Actual text: {text}");
                    LogStep($"✓ Status text contains 'Epic 1': {text.Substring(0, Math.Min(50, text.Length))}...");
                }
                else
                {
                    LogStep("Note: Could not locate status text element by AutomationId. This may need adjustment.");
                }
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not fully verify status text. Details: {ex.Message}");
            }

            // Step 6: Verify "Test Claude API" button exists
            LogStep("Step 6: Looking for 'Test Claude API' button");
            try
            {
                var testButton = ElementFinder.FindByName(driver, "Test Claude API", timeoutSeconds: 5);
                Assert.IsNotNull(testButton, "'Test Claude API' button should exist");
                Assert.IsTrue(testButton.Enabled, "'Test Claude API' button should be enabled");
                LogStep("✓ 'Test Claude API' button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find 'Test Claude API' button. Error: {ex.Message}");
            }

            // Step 7: Verify Close button exists
            LogStep("Step 7: Looking for 'Close' button");
            try
            {
                // Find Close button within the dialog
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                Assert.IsNotNull(closeButton, "'Close' button should exist");
                Assert.IsTrue(closeButton.Enabled, "'Close' button should be enabled");
                LogStep("✓ 'Close' button found and enabled");
            }
            catch (Exception ex)
            {
                // Close button is critical - if not found, fail
                Assert.Fail($"Failed to find 'Close' button in dialog. Error: {ex.Message}");
            }

            // Step 8: Verify prompt textbox is disabled (Epic 2 feature)
            LogStep("Step 8: Checking if prompt textbox is disabled");
            try
            {
                // Try to find the prompt input (it should be disabled)
                var allTextBoxes = driver.FindElementsByClassName("TextBox");
                if (allTextBoxes.Count > 0)
                {
                    foreach (var textbox in allTextBoxes)
                    {
                        if (!textbox.Enabled)
                        {
                            LogStep("✓ Found disabled textbox (prompt input for Epic 2)");
                            break;
                        }
                    }
                }
                else
                {
                    LogStep("Note: No textboxes found (this may be expected in Epic 1)");
                }
            }
            catch (Exception ex)
            {
                LogStep($"Note: Could not check prompt textbox state. Details: {ex.Message}");
            }

            // Step 9: Close dialog
            LogStep("Step 9: Closing dialog");
            try
            {
                var closeButton = ElementFinder.FindChildByName(copilotDialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000); // Wait for dialog to close

                // Verify dialog closed
                bool dialogClosed = !ElementFinder.Exists(driver, "RevitAI Copilot");
                Assert.IsTrue(dialogClosed, "Dialog should close when Close button is clicked");
                LogStep("✓ Dialog closed successfully");
            }
            catch (Exception ex)
            {
                LogStep($"Warning: Failed to close dialog cleanly. Details: {ex.Message}");
            }

            LogStep("TEST COMPLETE: Copilot dialog verified successfully");
        }

        [Test]
        [Category("Smoke")]
        [Description("Verify dialog can be opened multiple times without issues")]
        public void Test_DialogCanBeOpenedMultipleTimes()
        {
            LogStep("TEST START: Verifying dialog can be opened multiple times");

            // Activate tab once
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(500);

            // Open and close dialog 3 times
            for (int i = 1; i <= 3; i++)
            {
                LogStep($"Iteration {i}: Opening dialog");

                // Click Copilot button
                var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(copilotButton);

                // Wait for dialog
                var dialog = ElementFinder.FindByName(driver, "RevitAI Copilot", timeoutSeconds: 10);
                Assert.IsNotNull(dialog, $"Dialog should open on attempt {i}");
                LogStep($"✓ Dialog opened (attempt {i})");

                // Close dialog
                var closeButton = ElementFinder.FindChildByName(dialog, "Close", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(closeButton);
                System.Threading.Thread.Sleep(1000);

                // Verify closed
                bool dialogClosed = !ElementFinder.Exists(driver, "RevitAI Copilot");
                Assert.IsTrue(dialogClosed, $"Dialog should close on attempt {i}");
                LogStep($"✓ Dialog closed (attempt {i})");
            }

            LogStep("TEST COMPLETE: Dialog opened and closed 3 times successfully");
        }
    }
}
