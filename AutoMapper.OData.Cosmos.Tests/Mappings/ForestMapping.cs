using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class ForestMapping : Profile
{
	public ForestMapping()
	{
        CreateMap<MetadataKeyValue, MetadataKeyValueModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<Metadata, MetadataModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<Forest, ForestModel>()
			.ForMember(dest => dest.ForestName, opts => opts.MapFrom(src => src.Name))			
			.ForAllMembers(opts => opts.ExplicitExpansion());

    }
}
