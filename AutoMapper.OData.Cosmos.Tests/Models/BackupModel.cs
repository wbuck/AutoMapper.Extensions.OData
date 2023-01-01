namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record BackupModel
{
    public Guid Id { get; init; }
    public Guid ForestId { get; init; }
    public DateTimeOffset DateCreated { get; init; }
    public BackupLocationModel Location { get; init; } = default!;
    public ICollection<int> Values { get; init; } = new List<int>();
}