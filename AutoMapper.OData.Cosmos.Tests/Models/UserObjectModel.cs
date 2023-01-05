namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record UserObjectModel
{
    public UserModel User { get; init; } = default!;
}
