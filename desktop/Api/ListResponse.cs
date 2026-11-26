using System.Collections.Generic;

namespace CompanyBrain.Client.Api;

public sealed record ListResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Total { get; init; }
    public required int Limit { get; init; }
    public required int Offset { get; init; }
}
