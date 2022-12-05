using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class ObjectAttributeMapping : Profile
{
	public ObjectAttributeMapping()
	{
		CreateMap<FakeComplex, FakeComplexModel>()
			.ForMember(dest => dest.Name, opts => opts.MapFrom(src => src.FirstName))
			.ForAllMembers(opts => opts.ExplicitExpansion());

		CreateMap<ObjectAttribute, ObjectAttributeModel>()
			.ForAllMembers(opts => opts.ExplicitExpansion());
	}
}
