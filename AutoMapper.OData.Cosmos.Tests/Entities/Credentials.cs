namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record Credentials
{
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
}
