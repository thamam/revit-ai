using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium.Appium.Windows;

namespace RevitAI.UITests.Helpers
{
    /// <summary>
    /// Helper class for finding UI elements with better error messages and diagnostics
    /// </summary>
    public static class ElementFinder
    {
        /// <summary>
        /// Find element by name with detailed error message if not found
        /// </summary>
        public static WindowsElement FindByName(WindowsDriver<WindowsElement> driver, string name, int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;
            Exception lastException = null;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var element = driver.FindElementByName(name);
                    if (element != null && element.Displayed)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(500);
            }

            // If not found, provide diagnostic information
            var availableElements = GetAvailableElementNames(driver);
            throw new InvalidOperationException(
                $"Element with name '{name}' not found within {timeoutSeconds} seconds.\n" +
                $"Available elements: {string.Join(", ", availableElements.Take(20))}\n" +
                $"Last error: {lastException?.Message}");
        }

        /// <summary>
        /// Find element by AutomationId with detailed error message
        /// </summary>
        public static WindowsElement FindByAutomationId(WindowsDriver<WindowsElement> driver, string automationId, int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;
            Exception lastException = null;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var element = driver.FindElementByAccessibilityId(automationId);
                    if (element != null && element.Displayed)
                    {
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(500);
            }

            throw new InvalidOperationException(
                $"Element with AutomationId '{automationId}' not found within {timeoutSeconds} seconds.\n" +
                $"Last error: {lastException?.Message}");
        }

        /// <summary>
        /// Find child element within a parent element
        /// </summary>
        public static WindowsElement FindChildByName(WindowsElement parent, string name, int timeoutSeconds = 5)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var element = parent.FindElementByName(name);
                    if (element != null && element.Displayed)
                    {
                        return element;
                    }
                }
                catch
                {
                    // Continue waiting
                }

                Thread.Sleep(500);
            }

            throw new InvalidOperationException(
                $"Child element with name '{name}' not found within parent within {timeoutSeconds} seconds.");
        }

        /// <summary>
        /// Check if an element exists without throwing exception
        /// </summary>
        public static bool Exists(WindowsDriver<WindowsElement> driver, string name)
        {
            try
            {
                var element = driver.FindElementByName(name);
                return element != null && element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get list of available element names for diagnostics
        /// </summary>
        private static List<string> GetAvailableElementNames(WindowsDriver<WindowsElement> driver)
        {
            var names = new List<string>();
            try
            {
                var allElements = driver.FindElementsByXPath("//*[@Name]");
                foreach (var element in allElements.Take(50)) // Limit to first 50
                {
                    try
                    {
                        var name = element.GetAttribute("Name");
                        if (!string.IsNullOrEmpty(name))
                        {
                            names.Add(name);
                        }
                    }
                    catch
                    {
                        // Skip elements that can't be accessed
                    }
                }
            }
            catch
            {
                names.Add("(Could not enumerate elements)");
            }

            return names;
        }

        /// <summary>
        /// Wait for element to become enabled
        /// </summary>
        public static void WaitForEnabled(WindowsElement element, int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                if (element.Enabled)
                {
                    return;
                }

                Thread.Sleep(500);
            }

            throw new TimeoutException($"Element did not become enabled within {timeoutSeconds} seconds.");
        }

        /// <summary>
        /// Click element with retry logic (sometimes clicks fail on first attempt)
        /// </summary>
        public static void ClickWithRetry(WindowsElement element, int maxAttempts = 3)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    element.Click();
                    Thread.Sleep(500); // Brief pause after click
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < maxAttempts)
                    {
                        Thread.Sleep(1000); // Wait before retry
                    }
                }
            }

            throw new InvalidOperationException(
                $"Failed to click element after {maxAttempts} attempts. " +
                $"Last error: {lastException?.Message}", lastException);
        }
    }
}
