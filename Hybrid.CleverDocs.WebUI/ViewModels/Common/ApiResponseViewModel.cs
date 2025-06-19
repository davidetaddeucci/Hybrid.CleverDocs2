using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs.WebUI.ViewModels.Common
{
    /// <summary>
    /// Standard API response wrapper aligned with WebServices PaginatedResponse<T>
    /// </summary>
    /// <typeparam name="T">Type of the response data</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Whether the API call was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response data (null if unsuccessful)
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Response message (used for both success and error messages)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Current page number (1-based) for paginated responses
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page for paginated responses
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public long TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Create a successful response
        /// </summary>
        /// <param name="data">Response data</param>
        /// <param name="message">Success message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Successful ApiResponse</returns>
        public static ApiResponse<T> SuccessResponse(T data, string? message = null, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "Operation completed successfully",
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Create an error response
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Error ApiResponse</returns>
        public static ApiResponse<T> ErrorResponse(string errorMessage, int statusCode = 500)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = errorMessage,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Check if the response indicates a client error (4xx)
        /// </summary>
        public bool IsClientError => StatusCode >= 400 && StatusCode < 500;

        /// <summary>
        /// Check if the response indicates a server error (5xx)
        /// </summary>
        public bool IsServerError => StatusCode >= 500;
    }

    /// <summary>
    /// Non-generic API response for operations that don't return data
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>
        /// Create a successful response without data
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Successful ApiResponse</returns>
        public static ApiResponse SuccessResponse(string? message = null, int statusCode = 200)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message ?? "Operation completed successfully",
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Create an error response without data
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Error ApiResponse</returns>
        public new static ApiResponse ErrorResponse(string errorMessage, int statusCode = 500)
        {
            return new ApiResponse
            {
                Success = false,
                Message = errorMessage,
                StatusCode = statusCode
            };
        }
    }

}
