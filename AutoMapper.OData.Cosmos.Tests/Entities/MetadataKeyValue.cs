namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record MetadataKeyValue
{
    public string Key { get; init; } = default!;
    public int Value { get; init; }
}
