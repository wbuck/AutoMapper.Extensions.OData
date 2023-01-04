namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Entry
{
    public DomainController Dc { get; init; } = default!;
}
