namespace Hybrid.CleverDocs2.WebServices.Models.Common;

/// <summary>
/// Represents a paged result set with metadata
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page
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
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Index of the first item on the current page (0-based)
    /// </summary>
    public int StartIndex => (Page - 1) * PageSize;

    /// <summary>
    /// Index of the last item on the current page (0-based)
    /// </summary>
    public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalCount - 1);

    /// <summary>
    /// Number of items on the current page
    /// </summary>
    public int CurrentPageCount => Items.Count;

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a paged result with the specified parameters
    /// </summary>
    /// <param name="items">The items for the current page</param>
    /// <param name="totalCount">Total number of items</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    public PagedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items ?? new List<T>();
        TotalCount = totalCount;
        Page = Math.Max(1, page);
        PageSize = Math.Max(1, pageSize);
    }

    /// <summary>
    /// Creates a paged result from a full list of items
    /// </summary>
    /// <param name="allItems">All items to paginate</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paged result containing the requested page</returns>
    public static PagedResult<T> Create(List<T> allItems, int page, int pageSize)
    {
        var totalCount = allItems.Count;
        var startIndex = (page - 1) * pageSize;
        var items = allItems.Skip(startIndex).Take(pageSize).ToList();

        return new PagedResult<T>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>An empty paged result</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
    {
        return new PagedResult<T>(new List<T>(), 0, page, pageSize);
    }

    /// <summary>
    /// Maps the items to a different type
    /// </summary>
    /// <typeparam name="TResult">The target type</typeparam>
    /// <param name="mapper">Function to map each item</param>
    /// <returns>A new paged result with mapped items</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return new PagedResult<TResult>(mappedItems, TotalCount, Page, PageSize);
    }

    /// <summary>
    /// Gets pagination metadata
    /// </summary>
    /// <returns>Dictionary containing pagination information</returns>
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["page"] = Page,
            ["pageSize"] = PageSize,
            ["totalCount"] = TotalCount,
            ["totalPages"] = TotalPages,
            ["hasPreviousPage"] = HasPreviousPage,
            ["hasNextPage"] = HasNextPage,
            ["startIndex"] = StartIndex,
            ["endIndex"] = EndIndex,
            ["currentPageCount"] = CurrentPageCount
        };
    }
}

/// <summary>
/// Extension methods for creating paged results
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Converts an IQueryable to a paged result
    /// </summary>
    /// <typeparam name="T">The type of items</typeparam>
    /// <param name="query">The queryable source</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paged result</returns>
    public static PagedResult<T> ToPagedResult<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Converts an IEnumerable to a paged result
    /// </summary>
    /// <typeparam name="T">The type of items</typeparam>
    /// <param name="source">The enumerable source</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paged result</returns>
    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        return PagedResult<T>.Create(list, page, pageSize);
    }

    /// <summary>
    /// Converts a List to a paged result
    /// </summary>
    /// <typeparam name="T">The type of items</typeparam>
    /// <param name="source">The list source</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paged result</returns>
    public static PagedResult<T> ToPagedResult<T>(this List<T> source, int page, int pageSize)
    {
        return PagedResult<T>.Create(source, page, pageSize);
    }
}

/// <summary>
/// Request model for pagination parameters
/// </summary>
public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, Math.Min(100, value)); // Limit to max 100 items per page
    }

    /// <summary>
    /// Skip count for database queries
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Take count for database queries
    /// </summary>
    public int Take => PageSize;
}

/// <summary>
/// Response model for pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int CurrentPageCount { get; set; }

    public static PaginationMetadata FromPagedResult<T>(PagedResult<T> pagedResult)
    {
        return new PaginationMetadata
        {
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage,
            StartIndex = pagedResult.StartIndex,
            EndIndex = pagedResult.EndIndex,
            CurrentPageCount = pagedResult.CurrentPageCount
        };
    }
}
