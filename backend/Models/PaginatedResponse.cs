namespace TallyJ4.Models;

/// <summary>
/// Generic paginated response wrapper for collections that require pagination.
/// Provides metadata about the current page, total items, and navigation information.
/// </summary>
/// <typeparam name="T">The type of items in the paginated collection.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The items on the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Indicates whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates whether there is a next page available.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a new paginated response with the specified items and pagination metadata.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <returns>A new PaginatedResponse instance with the specified data.</returns>
    public static PaginatedResponse<T> Create(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
