using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record DomainControllerModel
{
    public Guid Id { get; init; } = default;
    public Guid ForestId { get; init; }
    public string FullyQualifiedDomainName { get; init; } = default!;
    public MetadataModel Metadata { get; init; } = default!;
    public BackupModel? SelectedBackup { get; init; }
    public ICollection<ObjectAttributeModel> Attributes { get; init; }
        = new List<ObjectAttributeModel>();
    public ICollection<BackupModel> Backups { get; init; } =
        new List<BackupModel>();
    public AdminGroupModel AdminGroup { get; init; } = default!;
    public FsmoRole[] FsmoRoles { get; init; }
        = Array.Empty<FsmoRole>();
    public DcStatusModel Status { get; init; }
}
