using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class NetworkInformationMapping : Profile
{
    public NetworkInformationMapping()
    {
        CreateMap<NetworkInformation, NetworkInformationModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());
    }
}
