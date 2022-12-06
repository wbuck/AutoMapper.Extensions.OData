using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Models;

public sealed record InternalFakeObjectModel
{
    public string? MyValue { get; init; }
}

public sealed record FakeObjectOneModel
{
    public int Value { get; init; }
    public InternalFakeObjectModel? InternalFakeObject { get; init; }
}

public sealed record FakeObjectTwoModel
{
    public int Value { get; init; }
}
internal sealed record AdObjectModel
{
    public DateTimeOffset DateAdded { get; init; }
    public DomainControllerModel Dc { get; init; } = default!;
    public FakeObjectOneModel? FakeObjectOne { get; init; }
    public FakeObjectTwoModel? FakeObjectTwo { get; init; }
}
