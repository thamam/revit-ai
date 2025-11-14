using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using RevitAI.UITests.Helpers;

namespace RevitAI.UITests
{
    /// <summary>
    /// Base class for all Revit UI tests.
    /// Handles Revit launch, WinAppDriver connection, and cleanup.
    /// </summary>
    [TestFixture]
    public abstract class BaseRevitUITest
    {
        protected WindowsDriver<WindowsElement> driver;
        protected Process revitProcess;
        protected WindowsElement revitWindow;

        // Configuration
        protected virtual string RevitExecutablePath => @"C:\Program Files\Autodesk\Revit 2024\Revit.exe";
        protected virtual string WinAppDriverUrl => "http://127.0.0.1:4723";
        protected virtual int RevitStartupTimeoutSeconds => 90; // Configurable timeout
        protected virtual int ElementWaitTimeoutSeconds => 30;

        /// <summary>
        /// Set up before each test: Launch Revit and connect WinAppDriver
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            TestContext.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting test: {TestContext.CurrentContext.Test.Name}");

            try
            {
                // Step 1: Connect to WinAppDriver session
                TestContext.WriteLine("[SetUp] Connecting to WinAppDriver...");
                ConnectToWinAppDriver();

                // Step 2: Launch Revit
                TestContext.WriteLine("[SetUp] Launching Revit...");
                LaunchRevit();

                // Step 3: Find Revit main window
                TestContext.WriteLine("[SetUp] Finding Revit main window...");
                revitWindow = FindRevitMainWindow();

                TestContext.WriteLine("[SetUp] Setup complete. Ready for test execution.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[SetUp] FATAL: Setup failed: {ex.Message}");
                TearDown(); // Clean up on failure
                throw;
            }
        }

        /// <summary>
        /// Tear down after each test: Close Revit and clean up resources
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            TestContext.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tearing down test: {TestContext.CurrentContext.Test.Name}");

            try
            {
                // Close any open dialogs first
                TryCloseOpenDialogs();

                // Close Revit
                if (revitProcess != null && !revitProcess.HasExited)
                {
                    TestContext.WriteLine("[TearDown] Closing Revit...");
                    revitProcess.CloseMainWindow();

                    // Wait for graceful exit
                    if (!revitProcess.WaitForExit(10000)) // 10 second timeout
                    {
                        TestContext.WriteLine("[TearDown] Revit did not exit gracefully, forcing kill...");
                        revitProcess.Kill();
                    }

                    revitProcess.Dispose();
                    revitProcess = null;
                }

                // Dispose WinAppDriver session
                if (driver != null)
                {
                    TestContext.WriteLine("[TearDown] Closing WinAppDriver session...");
                    driver.Quit();
                    driver.Dispose();
                    driver = null;
                }

                TestContext.WriteLine("[TearDown] Teardown complete.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[TearDown] Warning: Teardown error (non-fatal): {ex.Message}");
            }
        }

        #region Setup Helper Methods

        /// <summary>
        /// Connect to WinAppDriver session
        /// </summary>
        private void ConnectToWinAppDriver()
        {
            var options = new AppiumOptions();
            options.AddAdditionalCapability("app", "Root");
            options.AddAdditionalCapability("deviceName", "WindowsPC");
            options.AddAdditionalCapability("platformName", "Windows");

            try
            {
                driver = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), options);
                TestContext.WriteLine($"[WinAppDriver] Connected to {WinAppDriverUrl}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to WinAppDriver at {WinAppDriverUrl}. " +
                    "Ensure WinAppDriver.exe is running. " +
                    $"Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Launch Revit application
        /// </summary>
        private void LaunchRevit()
        {
            if (!System.IO.File.Exists(RevitExecutablePath))
            {
                throw new InvalidOperationException(
                    $"Revit executable not found at: {RevitExecutablePath}. " +
                    "Please update RevitExecutablePath in test configuration.");
            }

            revitProcess = Process.Start(new ProcessStartInfo
            {
                FileName = RevitExecutablePath,
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(RevitExecutablePath)
            });

            if (revitProcess == null)
            {
                throw new InvalidOperationException("Failed to start Revit process.");
            }

            TestContext.WriteLine($"[Revit] Launched Revit (PID: {revitProcess.Id}). Waiting for startup...");

            // Wait for Revit to be ready
            WaitForRevitStartup();
        }

        /// <summary>
        /// Wait for Revit to fully start up
        /// </summary>
        private void WaitForRevitStartup()
        {
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(RevitStartupTimeoutSeconds);

            while (stopwatch.Elapsed < timeout)
            {
                // Check if Revit main window exists
                try
                {
                    var window = driver.FindElementByName("Autodesk Revit 2024");
                    if (window != null && window.Displayed)
                    {
                        TestContext.WriteLine($"[Revit] Started successfully in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
                        return;
                    }
                }
                catch
                {
                    // Window not ready yet, continue waiting
                }

                Thread.Sleep(1000); // Check every second
            }

            throw new TimeoutException(
                $"Revit did not start within {RevitStartupTimeoutSeconds} seconds. " +
                "Consider increasing RevitStartupTimeoutSeconds if your machine is slow.");
        }

        /// <summary>
        /// Find the main Revit window
        /// </summary>
        private WindowsElement FindRevitMainWindow()
        {
            try
            {
                var window = driver.FindElementByName("Autodesk Revit 2024");

                if (window == null || !window.Displayed)
                {
                    throw new InvalidOperationException("Revit main window found but not visible");
                }

                // Bring window to foreground
                window.Click();
                Thread.Sleep(500); // Brief pause for focus

                TestContext.WriteLine("[Revit] Main window located and focused");
                return window;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not locate Revit main window. " +
                    "Ensure Revit is running and window is visible.", ex);
            }
        }

        #endregion

        #region Helper Methods for Tests

        /// <summary>
        /// Wait for an element to appear with custom timeout
        /// </summary>
        protected WindowsElement WaitForElement(Func<WindowsElement> finder, int timeoutSeconds = -1)
        {
            if (timeoutSeconds < 0)
                timeoutSeconds = ElementWaitTimeoutSeconds;

            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            Exception lastException = null;

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    var element = finder();
                    if (element != null && element.Displayed)
                    {
                        TestContext.WriteLine($"[Wait] Element found in {stopwatch.Elapsed.TotalSeconds:F1}s");
                        return element;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(500); // Check every 500ms
            }

            throw new TimeoutException(
                $"Element not found within {timeoutSeconds} seconds. " +
                $"Last error: {lastException?.Message}");
        }

        /// <summary>
        /// Try to find an element without throwing exception
        /// </summary>
        protected WindowsElement TryFindElement(Func<WindowsElement> finder)
        {
            try
            {
                return finder();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Close any open dialogs before teardown
        /// </summary>
        private void TryCloseOpenDialogs()
        {
            try
            {
                // Try to find and close RevitAI dialogs
                var closeButtons = driver.FindElementsByName("Close");
                foreach (var button in closeButtons)
                {
                    try
                    {
                        button.Click();
                        Thread.Sleep(500);
                    }
                    catch
                    {
                        // Ignore errors - dialog might already be closed
                    }
                }
            }
            catch
            {
                // Ignore all errors in cleanup
            }
        }

        #endregion

        #region Logging Helpers

        /// <summary>
        /// Log test step for better debugging
        /// </summary>
        protected void LogStep(string message)
        {
            TestContext.WriteLine($"[{DateTime.Now:HH:mm:ss}] [TEST] {message}");
        }

        #endregion
    }
}
