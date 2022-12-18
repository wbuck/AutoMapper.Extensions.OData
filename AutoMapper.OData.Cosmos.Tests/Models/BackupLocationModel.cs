namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record BackupLocationModel
{
    public CredentialsModel? Credentials { get; init; }
    public NetworkInformationModel? NetworkInformation { get; init; }
}


