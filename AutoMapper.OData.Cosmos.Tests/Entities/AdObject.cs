using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Entities;

public sealed record InternalFakeObject
{
    public string? MyValue { get; init; }
}

public sealed record FakeObjectOne
{
    public int Value { get; init; }
    public InternalFakeObject? InternalFakeObject { get; init; }
}

public sealed record FakeObjectTwo
{
    public int Value { get; init; }
}

public sealed record AdObject
{
    //public Guid Id { get; init; }
    public DateTimeOffset DateAdded { get; init; }
    public DomainController Dc { get; init; } = default!;
    public FakeObjectOne? FakeObjectOne { get; init; }
    public FakeObjectTwo? FakeObjectTwo { get; init; }
}


