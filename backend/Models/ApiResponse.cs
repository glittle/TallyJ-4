namespace TallyJ4.Models;

/// <summary>
/// Generic API response wrapper that standardizes the format of all API responses.
/// Provides consistent success/error handling with optional data and error messages.
/// </summary>
/// <typeparam name="T">The type of data contained in the response.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The data returned by the API operation, if successful.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// An optional message providing additional information about the response.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// A list of error messages, if the operation failed.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful API response with the specified data.
    /// </summary>
    /// <param name="data">The data to include in the response.</param>
    /// <param name="message">An optional success message.</param>
    /// <returns>A successful ApiResponse containing the data.</returns>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error API response with the specified message and optional error list.
    /// </summary>
    /// <param name="message">The primary error message.</param>
    /// <param name="errors">An optional list of detailed error messages.</param>
    /// <returns>An error ApiResponse with the specified message and errors.</returns>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an error API response with the specified list of error messages.
    /// </summary>
    /// <param name="errors">The list of error messages to include in the response.</param>
    /// <returns>An error ApiResponse containing the list of errors.</returns>
    public static ApiResponse<T> ErrorResponse(List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors
        };
    }
}
