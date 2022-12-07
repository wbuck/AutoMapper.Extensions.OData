namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record DomainControllerEntry
{
    public DateTimeOffset DateAdded { get; init; }
    public DomainController Dc { get; init; } = default!;
    public Credentials? DcCredentials { get; init; }
    public NetworkInformation? DcNetworkInformation { get; init; }
}


