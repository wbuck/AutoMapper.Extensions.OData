using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class BackupMapping : Profile
{
	public BackupMapping()
	{
		CreateMap<BackupLocation, BackupLocationModel>()
            .ForAllMembers(opts => opts.ExplicitExpansion());
        CreateMap<Backup, BackupModel>()           
            .ForAllMembers(opts => opts.ExplicitExpansion());
    }
}
