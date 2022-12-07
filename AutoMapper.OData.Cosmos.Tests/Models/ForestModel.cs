using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record ForestModel
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public string ForestName { get; init; } = default!;
    public CredentialsModel? ForestWideCredentials { get; init; } = default!;
    public ICollection<AdObjectModel> AdObjects { get; init; } =
        new List<AdObjectModel>();
}
