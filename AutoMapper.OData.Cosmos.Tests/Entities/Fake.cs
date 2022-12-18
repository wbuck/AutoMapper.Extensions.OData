using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Entities;

public class FakeInternal
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Fake
{
    public FakeInternal FakeInternal { get; set; }
    public DomainController FakeDC { get; set; }
}

public class FakeInternalModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class FakeModel
{
    public FakeInternalModel FakeInternal { get; set; }
    public DomainControllerModel FakeDC { get; set; }
}
