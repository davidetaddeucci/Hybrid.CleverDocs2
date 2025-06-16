namespace Hybrid.CleverDocs.WebUI.Models.Common;

/// <summary>
/// Generic paged result wrapper for API responses
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// List of items for the current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a paged result with items and pagination info
    /// </summary>
    public PagedResult(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items ?? new List<T>();
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
    }

    /// <summary>
    /// Creates a paged result from a subset of items
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var items = source.ToList();
        var totalCount = items.Count;
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        return new PagedResult<T>(pagedItems, page, pageSize, totalCount);
    }

    /// <summary>
    /// Maps the items to a different type while preserving pagination info
    /// </summary>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return new PagedResult<TResult>(mappedItems, Page, PageSize, TotalCount);
    }
}

/// <summary>
/// Pagination metadata for UI components
/// </summary>
public class PaginationViewModel
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Start item number for current page
    /// </summary>
    public int StartItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

    /// <summary>
    /// End item number for current page
    /// </summary>
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

    /// <summary>
    /// Gets page numbers to display in pagination UI
    /// </summary>
    public IEnumerable<int> GetPageNumbers(int maxPages = 5)
    {
        var startPage = Math.Max(1, CurrentPage - maxPages / 2);
        var endPage = Math.Min(TotalPages, startPage + maxPages - 1);
        
        // Adjust start page if we're near the end
        if (endPage - startPage + 1 < maxPages)
        {
            startPage = Math.Max(1, endPage - maxPages + 1);
        }

        return Enumerable.Range(startPage, endPage - startPage + 1);
    }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of the response data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> CreateError(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors ?? new List<string>()
        };
    }
}
