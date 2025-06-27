using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Models;

/// <summary>
/// Type alias for backward compatibility
/// </summary>
public class PaginationMetadata : ResponseMetadata
{
    /// <summary>
    /// Constructor for pagination metadata
    /// </summary>
    public PaginationMetadata(int page, int pageSize, int totalCount)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public PaginationMetadata() { }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// The response message
    /// </summary>
    /// <example>Request completed successfully</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Additional metadata about the response
    /// </summary>
    public ResponseMetadata? Metadata { get; set; }

    /// <summary>
    /// List of errors if any occurred
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    /// <example>2025-06-19T10:30:00Z</example>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string message = "Success", ResponseMetadata? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? [],
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ApiResponse<T> ValidationErrorResult(List<string> validationErrors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Validation failed",
            Errors = validationErrors,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Metadata for paginated responses
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// Total number of items
    /// </summary>
    /// <example>150</example>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    /// <example>1</example>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    /// <example>15</example>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    /// <example>true</example>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    /// <example>false</example>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    /// <example>250</example>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Additional metadata information
    /// </summary>
    public Dictionary<string, object>? AdditionalInfo { get; set; }
}

/// <summary>
/// Request model for product search
/// </summary>
public class ProductSearchRequest
{
    /// <summary>
    /// Search query (product name, description, etc.)
    /// </summary>
    /// <example>iPhone 15 Pro</example>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Filter by brand
    /// </summary>
    /// <example>Apple</example>
    public string? Brand { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    /// <example>500.00</example>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    /// <example>1500.00</example>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Marketplace to search in
    /// </summary>
    /// <example>kaspi</example>
    public string? Marketplace { get; set; } = "kaspi";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    /// <example>1</example>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Request model for scraping products
/// </summary>
public class ScrapeRequest
{
    /// <summary>
    /// Search query to scrape
    /// </summary>
    /// <example>Samsung Galaxy S24</example>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Marketplace to scrape from
    /// </summary>
    /// <example>kaspi</example>
    public string Marketplace { get; set; } = "kaspi";

    /// <summary>
    /// Maximum number of products to scrape (legacy property for backward compatibility)
    /// </summary>
    /// <example>10</example>
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Maximum number of pages to scrape
    /// </summary>
    /// <example>3</example>
    public int MaxPages { get; set; } = 1;

    /// <summary>
    /// Whether to use cache for results
    /// </summary>
    /// <example>true</example>
    public bool UseCache { get; set; } = true;
}
