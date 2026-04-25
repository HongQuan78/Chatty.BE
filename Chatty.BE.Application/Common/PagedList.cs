namespace Chatty.BE.Application.Common;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
public class PagedList<T>(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int PageIndex { get; } = pageIndex;
    public int PageSize { get; } = pageSize;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static PagedList<T> Create(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        return new PagedList<T>(items, totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// Maps the PagedList to a different type.
    /// </summary>
    public PagedList<TDestination> Map<TDestination>(Func<T, TDestination> converter)
    {
        var mappedItems = Items.Select(converter).ToList();
        return new PagedList<TDestination>(mappedItems, TotalCount, PageIndex, PageSize);
    }
}
