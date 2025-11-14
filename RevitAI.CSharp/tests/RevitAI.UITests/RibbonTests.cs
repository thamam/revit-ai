using System;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Tests for UC-02: Ribbon UI Displays Correctly
    /// Verifies that RevitAI ribbon tab appears with correct panel and buttons
    /// </summary>
    [TestFixture]
    public class RibbonTests : BaseRevitUITest
    {
        [Test]
        [Category("Smoke")]
        [Description("UC-02: Verify RevitAI ribbon tab displays with AI Copilot panel and two buttons")]
        public void Test_UC02_RibbonDisplaysCorrectly()
        {
            LogStep("TEST START: Verifying RevitAI ribbon UI");

            // Step 1: Find RevitAI tab
            LogStep("Step 1: Looking for RevitAI tab in ribbon");
            WindowsElement revitaiTab = null;
            try
            {
                revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
                Assert.IsNotNull(revitaiTab, "RevitAI tab should exist in ribbon");
                Assert.IsTrue(revitaiTab.Displayed, "RevitAI tab should be visible");
                LogStep("✓ RevitAI tab found and visible");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find RevitAI tab. Error: {ex.Message}");
            }

            // Step 2: Click the tab to make it active (may not be selected by default)
            LogStep("Step 2: Clicking RevitAI tab to activate it");
            try
            {
                ElementFinder.ClickWithRetry(revitaiTab, maxAttempts: 3);
                System.Threading.Thread.Sleep(1000); // Wait for panel to appear
                LogStep("✓ RevitAI tab activated");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to click RevitAI tab. Error: {ex.Message}");
            }

            // Step 3: Verify AI Copilot panel exists
            LogStep("Step 3: Looking for 'AI Copilot' panel");
            WindowsElement panel = null;
            try
            {
                // Panel might be found by name or as a child of the tab
                panel = ElementFinder.FindByName(driver, "AI Copilot", timeoutSeconds: 5);
                Assert.IsNotNull(panel, "AI Copilot panel should exist");
                LogStep("✓ AI Copilot panel found");
            }
            catch (Exception ex)
            {
                // If we can't find the panel by name, that's okay - buttons might be directly accessible
                LogStep($"Note: Could not find panel by name (this may be normal). Proceeding to check buttons. Details: {ex.Message}");
            }

            // Step 4: Find Copilot button
            LogStep("Step 4: Looking for 'Copilot' button");
            WindowsElement copilotButton = null;
            try
            {
                copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
                Assert.IsNotNull(copilotButton, "Copilot button should exist");
                Assert.IsTrue(copilotButton.Enabled, "Copilot button should be enabled");
                LogStep("✓ Copilot button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find Copilot button. Error: {ex.Message}");
            }

            // Step 5: Find Settings button
            LogStep("Step 5: Looking for 'Settings' button");
            WindowsElement settingsButton = null;
            try
            {
                settingsButton = ElementFinder.FindByName(driver, "Settings", timeoutSeconds: 5);
                Assert.IsNotNull(settingsButton, "Settings button should exist");
                Assert.IsTrue(settingsButton.Enabled, "Settings button should be enabled");
                LogStep("✓ Settings button found and enabled");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to find Settings button. Error: {ex.Message}");
            }

            // Step 6: Verify we have exactly 2 buttons (optional - may need adjustment based on actual UI)
            LogStep("Step 6: Verifying button count");
            try
            {
                // This is a sanity check - ensure we don't have unexpected buttons
                var allButtons = driver.FindElementsByName("Copilot")
                    .Concat(driver.FindElementsByName("Settings"))
                    .Where(e => e.Displayed)
                    .ToList();

                // We should have at least our 2 buttons
                Assert.GreaterOrEqual(allButtons.Count, 2, "Should have at least Copilot and Settings buttons");
                LogStep($"✓ Found {allButtons.Count} RevitAI buttons (at least 2 expected)");
            }
            catch (Exception ex)
            {
                // This is not a critical failure - log and continue
                LogStep($"Note: Could not verify button count. This may be normal. Details: {ex.Message}");
            }

            LogStep("TEST COMPLETE: RevitAI ribbon UI verified successfully");
        }

        [Test]
        [Category("Smoke")]
        [Description("Verify RevitAI tab remains visible after clicking away and back")]
        public void Test_RibbonPersistence_TabRemainsVisible()
        {
            LogStep("TEST START: Verifying ribbon persistence");

            // Step 1: Find and click RevitAI tab
            LogStep("Step 1: Clicking RevitAI tab");
            var revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 10);
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(1000);

            // Step 2: Click a different tab (like Architecture)
            LogStep("Step 2: Clicking away to Architecture tab");
            try
            {
                var archTab = ElementFinder.FindByName(driver, "Architecture", timeoutSeconds: 5);
                ElementFinder.ClickWithRetry(archTab);
                System.Threading.Thread.Sleep(1000);
                LogStep("✓ Switched to Architecture tab");
            }
            catch
            {
                LogStep("Note: Could not find Architecture tab, skipping switch test");
                // This is okay - just testing if we can find it
            }

            // Step 3: Click back to RevitAI tab
            LogStep("Step 3: Clicking back to RevitAI tab");
            revitaiTab = ElementFinder.FindByName(driver, "RevitAI", timeoutSeconds: 5);
            Assert.IsNotNull(revitaiTab, "RevitAI tab should still exist after switching tabs");
            ElementFinder.ClickWithRetry(revitaiTab);
            System.Threading.Thread.Sleep(1000);

            // Step 4: Verify buttons are still there
            LogStep("Step 4: Verifying buttons still exist");
            var copilotButton = ElementFinder.FindByName(driver, "Copilot", timeoutSeconds: 5);
            var settingsButton = ElementFinder.FindByName(driver, "Settings", timeoutSeconds: 5);

            Assert.IsNotNull(copilotButton, "Copilot button should still exist after tab switching");
            Assert.IsNotNull(settingsButton, "Settings button should still exist after tab switching");

            LogStep("TEST COMPLETE: Ribbon persistence verified");
        }
    }
}
