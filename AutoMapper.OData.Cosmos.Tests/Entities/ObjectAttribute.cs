namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record ObjectAttribute
{
    //public Guid Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Value { get; init; } = default!;
    public FakeComplex FakeComplex { get; init; } = default!;
}
