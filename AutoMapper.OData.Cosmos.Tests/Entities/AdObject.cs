namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record AdObject
{
    //public Guid Id { get; init; }
    public DateTimeOffset DateAdded { get; init; }
    public DomainController Dc { get; init; } = default!;
}


