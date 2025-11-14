using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Revit ExternalEvent Handler
    /// Enables thread-safe Revit API access from background threads
    ///
    /// Story 1.3: ExternalEvent Pattern for Thread-Safe Revit API Access
    /// </summary>
    public class RevitEventHandler : IExternalEventHandler
    {
        private static RevitEventHandler _instance;
        private static ExternalEvent _externalEvent;
        private static readonly ConcurrentQueue<RevitRequest> _requestQueue = new ConcurrentQueue<RevitRequest>();

        /// <summary>
        /// Initialize the singleton instance
        /// Must be called from Revit main thread during startup
        /// </summary>
        public static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new RevitEventHandler();
                _externalEvent = ExternalEvent.Create(_instance);
            }
        }

        /// <summary>
        /// Enqueue a request and wait for it to be processed
        /// Can be called from any thread
        /// </summary>
        public static async Task<RevitResponse> EnqueueRequestAsync(RevitAction action, TimeSpan? timeout = null)
        {
            if (_externalEvent == null)
            {
                return RevitResponse.CreateFailure(
                    "RevitEventHandler not initialized. Call Initialize() during application startup.");
            }

            var request = new RevitRequest { Action = action };
            _requestQueue.Enqueue(request);

            var logger = LoggingService.Instance;
            logger.Info($"Enqueued request {request.RequestId}, raising ExternalEvent", "EXTERNAL_EVENT");

            // Raise the external event to trigger Execute() on Revit main thread
            var raiseResult = _externalEvent.Raise();
            logger.Info($"ExternalEvent.Raise() returned: {raiseResult}", "EXTERNAL_EVENT");

            // Wait for the request to be processed (with timeout)
            var timeoutSpan = timeout ?? TimeSpan.FromSeconds(30);
            try
            {
                logger.Info($"Waiting for response (timeout: {timeoutSpan.TotalSeconds}s)", "EXTERNAL_EVENT");
                return await request.CompletionSource.Task.WaitAsync(timeoutSpan);
            }
            catch (TimeoutException)
            {
                logger.Error($"Request {request.RequestId} timed out after {timeoutSpan.TotalSeconds}s", "EXTERNAL_EVENT");
                return RevitResponse.CreateFailure(
                    $"Operation timed out after {timeoutSpan.TotalSeconds} seconds. " +
                    $"ExternalEvent.Raise() returned: {raiseResult}. " +
                    "Check logs for details. Revit may be busy or Execute() was not called.");
            }
        }

        /// <summary>
        /// Execute method called by Revit on main thread
        /// Processes all queued requests
        /// </summary>
        public void Execute(UIApplication app)
        {
            try
            {
                var logger = LoggingService.Instance;
                logger.Info($"Execute() called by Revit, queue count: {_requestQueue.Count}", "EXTERNAL_EVENT");

                while (_requestQueue.TryDequeue(out RevitRequest request))
                {
                    try
                    {
                        logger.Info($"Processing request: {request.RequestId}", "EXTERNAL_EVENT");
                        logger.LogOperation(request.Action.Operation, "STARTED", $"RequestId: {request.RequestId}");

                        var response = ProcessRequest(app, request.Action);
                        request.CompletionSource.SetResult(response);

                        string status = response.Success ? "SUCCESS" : "FAILED";
                        logger.LogOperation(request.Action.Operation, status, response.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Request {request.RequestId} failed", "EXTERNAL_EVENT", ex);

                        var errorResponse = RevitResponse.CreateFailure(
                            $"Revit operation failed: {ex.Message}",
                            ex.StackTrace);
                        request.CompletionSource.SetResult(errorResponse);

                        logger.LogOperation(request.Action.Operation, "ERROR", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any exception that occurs before we can access logger
                System.Diagnostics.Debug.WriteLine($"CRITICAL: Execute() failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                // Try to log if possible
                try
                {
                    LoggingService.Instance.Error($"Execute() method failed critically", "EXTERNAL_EVENT", ex);
                }
                catch
                {
                    // If logging also fails, we're in big trouble - but at least Debug output will show it
                }
            }
        }

        /// <summary>
        /// Process a single request on Revit main thread
        /// </summary>
        private RevitResponse ProcessRequest(UIApplication app, RevitAction action)
        {
            // For now, just return a placeholder response
            // Actual operation execution will be implemented in Epic 2

            // Validate action first
            var validator = new SafetyValidator();
            var validation = validator.Validate(action, elementCount: 0);

            if (!validation.IsValid)
            {
                return RevitResponse.CreateFailure($"Safety validation failed: {validation.Message}");
            }

            // Placeholder: In Epic 2, this will execute actual Revit operations
            // based on action.Operation ("create_dimensions", "create_tags", "read_elements")

            return RevitResponse.CreateSuccess(
                $"Operation '{action.Operation}' validated successfully. " +
                "Execution will be implemented in Epic 2.",
                new { operation = action.Operation, validated = true });
        }

        /// <summary>
        /// Required by IExternalEventHandler
        /// </summary>
        public string GetName()
        {
            return "RevitAI Event Handler";
        }

        /// <summary>
        /// Test method: Simple operation to verify ExternalEvent works
        /// </summary>
        public static async Task<RevitResponse> TestEventHandlerAsync()
        {
            var testAction = new RevitAction
            {
                Operation = "read_elements",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new System.Collections.Generic.Dictionary<string, object>()
            };

            return await EnqueueRequestAsync(testAction);
        }
    }
}
