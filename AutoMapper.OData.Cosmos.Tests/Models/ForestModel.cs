using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record ForestModel
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public string ForestName { get; init; } = default!;
    public CredentialsModel? ForestWideCredentials { get; init; } = default!;
    public MetadataModel Metadata { get; init; } = default!;
    public DateTime CreatedDate { get; init; }
    public DomainControllerModel PrimaryDc { get; init; } = default!;
    public ICollection<DomainControllerEntryModel> DomainControllers { get; init; } =
        new List<DomainControllerEntryModel>();
    public ICollection<int> Values { get; init; } = 
        new List<int>();
    public ForestStatusModel Status { get; init; }
}
