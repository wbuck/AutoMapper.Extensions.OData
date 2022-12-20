using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record DomainControllerModel
{
    public Guid Id { get; init; } = default;
    public Guid ForestId { get; init; }
    public string FullyQualifiedDomainName { get; init; } = default!;
    public MetadataModel Metadata { get; init; } = default!;
    public ICollection<ObjectAttributeModel> Attributes { get; init; }
        = new List<ObjectAttributeModel>();
    public ICollection<BackupModel> Backups { get; init; } =
        new List<BackupModel>();

    public ICollection<FsmoRole> FsmoRoles { get; init; } =
         new List<FsmoRole>();
}
