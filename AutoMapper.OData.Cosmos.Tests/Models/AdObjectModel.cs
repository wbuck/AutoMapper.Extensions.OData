namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record AdObjectModel
{
    //public Guid Id { get; init; }
    public DateTimeOffset DateAdded { get; init; }
    public DomainControllerModel Dc { get; init; } = default!;
}
