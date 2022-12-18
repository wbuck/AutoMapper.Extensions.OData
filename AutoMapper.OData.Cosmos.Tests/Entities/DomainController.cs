namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record DomainController
{
    public Guid Id { get; init; } = default;
    public Guid ForestId { get; init; }
    public string Fqdn { get; init; } = default!;
    public Fake Fake { get; init; }
    public ICollection<ObjectAttribute> Attributes { get; init; } 
        = new List<ObjectAttribute>();
    public ICollection<Backup> Backups { get; init; } 
        = new List<Backup>();
    public ICollection<FsmoRole> FsmoRoles { get; init; } 
        = new List<FsmoRole>();
}


