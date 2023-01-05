using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class UserMapping : Profile
{
	public UserMapping()
	{
        CreateMap<AdminGroup, AdminGroupModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<UserObject, UserObjectModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());

        CreateMap<User, UserModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());
    }
}
