using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class ObjectAttributeMapping : Profile
{
	public ObjectAttributeMapping()
	{
        CreateMap<InternalFakeObject, InternalFakeObjectModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<FakeObjectOne, FakeObjectOneModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<FakeObjectTwo, FakeObjectTwoModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<AnotherFakeComplexType, AnotherFakeComplexTypeModel>()
			.ForAllMembers(opts => opts.ExplicitExpansion());

		CreateMap<FakeComplex, FakeComplexModel>()
			.ForMember(dest => dest.Name, opts => opts.MapFrom(src => src.FirstName))
			.ForAllMembers(opts => opts.ExplicitExpansion());

		CreateMap<ObjectAttribute, ObjectAttributeModel>()
			.ForAllMembers(opts => opts.ExplicitExpansion());
	}
}
