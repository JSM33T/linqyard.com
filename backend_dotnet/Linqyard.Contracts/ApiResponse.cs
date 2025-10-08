namespace Linqyard.Contracts;

public sealed record ApiResponse<T>(T Data, object? Meta = null);
public sealed record PagedMeta(int Page, int PageSize, long Total);
