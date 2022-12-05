using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class AdObjectMapping : Profile
{
	public AdObjectMapping()
	{
		CreateMap<AdObject, AdObjectModel>()            
            .ForAllMembers(opts => opts.ExplicitExpansion());
    }
}
