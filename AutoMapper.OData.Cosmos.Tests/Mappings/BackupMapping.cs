using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Models;

namespace AutoMapper.OData.Cosmos.Tests.Mappings;

internal sealed class BackupMapping : Profile
{
	public BackupMapping()
	{
		CreateMap<Backup, BackupModel>()
			.ForMember(dest => dest.PathToBackup, opts => opts.MapFrom(src => src.Path))            
            .ForAllMembers(opts => opts.ExplicitExpansion());
    }
}
