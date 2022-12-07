namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record CredentialsModel
{
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
}
