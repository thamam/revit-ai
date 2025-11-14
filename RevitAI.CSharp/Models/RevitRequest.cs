using System.Threading.Tasks;

namespace RevitAI.Models
{
    /// <summary>
    /// Represents a request to execute a Revit API operation
    /// Used for thread-safe communication via ExternalEvent
    /// </summary>
    public class RevitRequest
    {
        /// <summary>
        /// Unique identifier for this request
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// The action to execute (parsed from natural language)
        /// </summary>
        public RevitAction Action { get; set; }

        /// <summary>
        /// Completion source for async/await pattern
        /// Allows background thread to wait for Revit main thread to finish
        /// </summary>
        public TaskCompletionSource<RevitResponse> CompletionSource { get; set; }

        public RevitRequest()
        {
            RequestId = System.Guid.NewGuid().ToString();
            CompletionSource = new TaskCompletionSource<RevitResponse>();
        }
    }

    /// <summary>
    /// Response from a Revit API operation
    /// </summary>
    public class RevitResponse
    {
        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// User-friendly message (success or error)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Result data (if any)
        /// For create_dimensions: count of dimensions created
        /// For read_elements: element properties
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Exception details (if operation failed)
        /// </summary>
        public string ErrorDetails { get; set; }

        public static RevitResponse CreateSuccess(string message, object result = null)
        {
            return new RevitResponse
            {
                Success = true,
                Message = message,
                Result = result
            };
        }

        public static RevitResponse CreateFailure(string message, string errorDetails = null)
        {
            return new RevitResponse
            {
                Success = false,
                Message = message,
                ErrorDetails = errorDetails
            };
        }
    }
}
