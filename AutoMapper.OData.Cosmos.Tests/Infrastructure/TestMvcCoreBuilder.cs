using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace AutoMapper.OData.Cosmos.Tests.Infrastructure;

internal sealed class TestMvcCoreBuilder : IMvcCoreBuilder
{
    public ApplicationPartManager PartManager { get; set; }
    public IServiceCollection Services { get; set; }
}
