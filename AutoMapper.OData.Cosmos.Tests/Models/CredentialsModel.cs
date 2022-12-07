namespace AutoMapper.OData.Cosmos.Tests.Models;

internal sealed record CredentialsModel
{
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
}
