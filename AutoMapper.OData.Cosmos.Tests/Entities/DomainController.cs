namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record DomainController : EntityBase
{    
    public string Fqdn { get; init; } = default!;
    public Metadata Metadata { get; init; } = default!;
    public Backup? SelectedBackup { get; init; }
    public ICollection<ObjectAttribute> Attributes { get; init; } 
        = new List<ObjectAttribute>();
    public ICollection<Backup> Backups { get; init; } 
        = new List<Backup>();
    public AdminGroup AdminGroup { get; init; } = default!;
    public FsmoRole[] FsmoRoles { get; init; } 
        = Array.Empty<FsmoRole>();
    public DcStatus Status { get; init; }
}


