using Newtonsoft.Json;

namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Forest
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public string Name { get; init; } = default!;
    public FakeComplex FakeType { get; init; } = default!;
    public ICollection<AdObject> AdObjects { get; init; } = 
        new List<AdObject>();

    [JsonProperty("_etag")]
    public string ETag { get; init; } = default!;
}


