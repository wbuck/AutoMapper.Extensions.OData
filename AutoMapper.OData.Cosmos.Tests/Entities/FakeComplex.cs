namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record FakeComplex
{
    public string FirstName { get; init; } = null!;
    public AnotherFakeComplexType? AnotherFakeType { get; init; }
}
