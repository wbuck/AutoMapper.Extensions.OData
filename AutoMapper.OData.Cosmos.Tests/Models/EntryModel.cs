namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record EntryModel
{
    public DomainControllerModel Dc { get; init; } = default!;
}
