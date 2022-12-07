namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Backup
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public string Path { get; init; } = default!;
    public DateTimeOffset DateCreated { get; init; }
    public Credentials? Credentials { get; init; }
    public NetworkInformation? NetworkInformation { get; init; }
}


