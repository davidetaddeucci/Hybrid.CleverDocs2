using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs.WebUI.ViewModels.Common
{
    /// <summary>
    /// ViewModel for pagination functionality across all list views
    /// </summary>
    public class PaginationViewModel
    {
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Starting item number for current page (1-based)
        /// </summary>
        public int StartItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

        /// <summary>
        /// Ending item number for current page (1-based)
        /// </summary>
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        /// <summary>
        /// Get page numbers to display in pagination controls
        /// </summary>
        /// <param name="maxPagesToShow">Maximum number of page links to show</param>
        /// <returns>List of page numbers to display</returns>
        public IEnumerable<int> GetPageNumbers(int maxPagesToShow = 10)
        {
            if (TotalPages <= maxPagesToShow)
            {
                return Enumerable.Range(1, TotalPages);
            }

            var half = maxPagesToShow / 2;
            var start = Math.Max(1, CurrentPage - half);
            var end = Math.Min(TotalPages, start + maxPagesToShow - 1);

            // Adjust start if we're near the end
            if (end - start + 1 < maxPagesToShow)
            {
                start = Math.Max(1, end - maxPagesToShow + 1);
            }

            return Enumerable.Range(start, end - start + 1);
        }

        /// <summary>
        /// Create pagination info for display
        /// </summary>
        /// <returns>Formatted pagination info string</returns>
        public string GetPaginationInfo()
        {
            if (TotalItems == 0)
                return "No items found";

            return $"Showing {StartItem}-{EndItem} of {TotalItems} items";
        }

        /// <summary>
        /// Create a new PaginationViewModel with updated page
        /// </summary>
        /// <param name="newPage">New page number</param>
        /// <returns>New PaginationViewModel instance</returns>
        public PaginationViewModel WithPage(int newPage)
        {
            return new PaginationViewModel
            {
                CurrentPage = Math.Max(1, Math.Min(newPage, TotalPages)),
                PageSize = PageSize,
                TotalItems = TotalItems
            };
        }

        /// <summary>
        /// Create a new PaginationViewModel with updated page size
        /// </summary>
        /// <param name="newPageSize">New page size</param>
        /// <returns>New PaginationViewModel instance</returns>
        public PaginationViewModel WithPageSize(int newPageSize)
        {
            var newTotalPages = (int)Math.Ceiling((double)TotalItems / newPageSize);
            var newCurrentPage = Math.Min(CurrentPage, newTotalPages);

            return new PaginationViewModel
            {
                CurrentPage = Math.Max(1, newCurrentPage),
                PageSize = Math.Max(1, Math.Min(newPageSize, 100)),
                TotalItems = TotalItems
            };
        }
    }

    /// <summary>
    /// Generic paginated result wrapper
    /// </summary>
    /// <typeparam name="T">Type of items in the result</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Items for the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

        /// <summary>
        /// Create a new paged result
        /// </summary>
        /// <param name="items">Items for current page</param>
        /// <param name="totalItems">Total number of items</param>
        /// <param name="currentPage">Current page number</param>
        /// <param name="pageSize">Page size</param>
        public PagedResult(IEnumerable<T> items, int totalItems, int currentPage, int pageSize)
        {
            Items = items ?? new List<T>();
            Pagination = new PaginationViewModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        /// <summary>
        /// Create an empty paged result
        /// </summary>
        public PagedResult()
        {
        }
    }
}
