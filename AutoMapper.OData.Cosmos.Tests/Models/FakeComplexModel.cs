namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record FakeComplexModel
{
    public string Name { get; init; } = null!;
    public AnotherFakeComplexTypeModel? AnotherFakeType { get; init; }
}
