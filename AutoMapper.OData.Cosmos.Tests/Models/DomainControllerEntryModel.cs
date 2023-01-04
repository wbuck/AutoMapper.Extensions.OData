namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record DomainControllerEntryModel
{
    public DateTimeOffset DateAdded { get; init; }
    public EntryModel Entry { get; init; } = default!;
    public CredentialsModel? DcCredentials { get; init; }
    public NetworkInformationModel? DcNetworkInformation { get; init; }
}
