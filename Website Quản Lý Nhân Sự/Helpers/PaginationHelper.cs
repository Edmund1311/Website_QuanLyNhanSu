using System.ComponentModel.DataAnnotations;

namespace Website_Quản_Lý_Nhân_Sự.Helpers;

/// <summary>
/// Pagination helper class for managing paginated results
/// </summary>
public class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int ValidatePageNumber(int page)
    {
        return page < 1 ? 1 : page;
    }

    public static int ValidatePageSize(int pageSize)
    {
        if (pageSize < 1) return DefaultPageSize;
        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedResult() { }

    public PaginatedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
