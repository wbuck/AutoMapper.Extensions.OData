namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record ObjectAttributeModel
{
    //public Guid Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Value { get; init; } = default!;
    public FakeComplexModel FakeComplex { get; init; } = default!;
}
