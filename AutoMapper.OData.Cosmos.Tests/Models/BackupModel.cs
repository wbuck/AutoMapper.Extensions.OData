namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record BackupModel
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public string PathToBackup { get; init; } = default!;
    public DateTimeOffset DateCreated { get; init; }
    public CredentialsModel? Credentials { get; init; }
    public NetworkInformationModel? NetworkInformation { get; init; }
}