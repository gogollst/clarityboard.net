namespace ClarityBoard.Application.Common.Models;

public abstract record PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}
