namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record DomainController : EntityBase
{    
    public Guid ForestId { get; init; }
    public string Fqdn { get; init; } = default!;
    public Metadata Metadata { get; init; } = default!;
    public ICollection<ObjectAttribute> Attributes { get; init; } 
        = new List<ObjectAttribute>();
    public ICollection<Backup> Backups { get; init; } 
        = new List<Backup>();
    public ICollection<FsmoRole> FsmoRoles { get; init; } 
        = new List<FsmoRole>();
}


