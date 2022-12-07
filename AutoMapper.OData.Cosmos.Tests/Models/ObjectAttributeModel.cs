namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record ObjectAttributeModel
{
    public string Name { get; init; } = default!;
    public string Value { get; init; } = default!;
}
