namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Backup
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public DateTimeOffset DateCreated { get; init; }
    public BackupLocation Location { get; init; } = default!;
}


