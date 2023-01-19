using Newtonsoft.Json;

namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Forest : EntityBase
{    
    public string Name { get; init; } = default!;
    public Credentials? ForestWideCredentials { get; init; } = default!;
    public Metadata Metadata { get; init; } = default!;
    public DateTime CreatedDate { get; init; }
    public DomainController PrimaryDc { get; init; } = default!;
    public ICollection<DomainControllerEntry> DomainControllers { get; init; } = 
        new List<DomainControllerEntry>();
    public ICollection<int> Values { get; init; } =
        new List<int>();

    public ForestStatus Status { get; init; }

    [JsonProperty("_etag")]
    public string ETag { get; init; } = default!;
}


