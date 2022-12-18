namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record ObjectAttributeModel
{
    public string Name { get; init; } = default!;
    public string Value { get; init; } = default!;
}
