using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using System.Reflection;

namespace AutoMapper.OData.Cosmos.Tests;


[Collection(nameof(CosmosContainer))]
public sealed class GetQuerySelectTests
{	
	private readonly CosmosContainer dbContainer;
	private readonly IServiceProvider serviceProvider;

	public GetQuerySelectTests(CosmosContainer dbContainer)
	{
		this.dbContainer = dbContainer;

        IServiceCollection services = new ServiceCollection();
        IMvcCoreBuilder builder = new TestMvcCoreBuilder
        {
            Services = services
        };

		builder.AddOData();
		services
			.AddSingleton<IConfigurationProvider>(new MapperConfiguration(cfg => cfg.AddMaps(typeof(IAssemablyMarker).Assembly)))
            .AddTransient<IMapper>(sp => new Mapper(sp.GetRequiredService<IConfigurationProvider>(), sp.GetService))
            .AddTransient<IApplicationBuilder>(sp => new ApplicationBuilder(sp))
            .AddRouting()
            .AddLogging();

        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ForestNoSelects_NavigationPropertiesShouldNotBeExpanded_ComplexTypesShouldBeExpanded()
    {
        const string query = "/forest?$orderby=ForestName";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);

            foreach (var (model, forestName) in
                collection.Zip(new[] { "Abernathy Forest", "Rolfson Forest", "Zulauf Forest" }))
            {
                AssertModel(model, forestName);
            }
        }

        static void AssertModel(ForestModel model, string forestName)
        {
            Assert.Equal(forestName, model.ForestName);
            Assert.NotEqual(default, model.ForestId);
            Assert.NotEqual(default, model.Id);
            Assert.Equal(3, model.Values.Count);

            AssertDomainControllerEntry(model.DomainControllers);
            AssertMetadata(model.Metadata);
            AssertCredentials(model.ForestWideCredentials);                      
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.Null(model.Entry.Dc);
                Assert.NotEqual(default, model.DateAdded);
                AssertCredentials(model.DcCredentials);
                AssertNetworkInfo(model.DcNetworkInformation);
            }
        }

        static void AssertMetadata(MetadataModel model)
        {
            Assert.NotNull(model.MetadataType);
            Assert.Equal(3, model.MetadataKeyValuePairs.Count);
        }

        static void AssertNetworkInfo(params NetworkInformationModel?[] models)
        {
            foreach (var model in models)
            {
                Assert.NotNull(model);
                Assert.NotNull(model.Address);                
            }
        }

        static void AssertCredentials(params CredentialsModel?[] models)
        {
            foreach (var model in models)
            {
                Assert.NotNull(model);
                Assert.NotNull(model.Username);
                Assert.NotNull(model.Password);
            }
        }
    }    

    [Fact]
	public async Task ForestSelectForestNameExpandDc_DcShouldBeExpanded_RootComplexTypesShouldNotBeExpanded()
	{        
        const string query = "/forest?$select=ForestName&$expand=DomainControllers/Entry/Dc&$orderby=ForestName desc";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);

            foreach (var (model, forestName) in 
                collection.Zip(new[] { "Zulauf Forest", "Rolfson Forest", "Abernathy Forest" }))
            {
                AssertModel(model, forestName);
            }
        }

        static void AssertModel(ForestModel model, string forestName)
        {
            Assert.Equal(forestName, model.ForestName);
            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.Id);
            Assert.NotEmpty(model.DomainControllers);
            Assert.Equal(0, model.Values.Count);
            Assert.Null(model.Metadata);
            Assert.Null(model.ForestWideCredentials);

            AssertDomainControllerEntry(model.DomainControllers);
            AssertDomainController(model.DomainControllers.Select(m => m.Entry.Dc));
        }        

        static void AssertDomainController(IEnumerable<DomainControllerModel> models)
        {
            foreach (var model in models)
            {
                Assert.NotEqual(default, model.Id);
                Assert.NotEqual(default, model.ForestId);
                Assert.NotNull(model.FullyQualifiedDomainName);
                AssertMetadata(model.Metadata);
                AssertAttributes(model.Attributes);
                Assert.Equal(0, model.Backups.Count);
                Assert.NotEmpty(model.FsmoRoles);
            }
        }

        static void AssertAttributes(ICollection<ObjectAttributeModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.NotNull(model.Name);
                Assert.NotNull(model.Value);
            }
        }

        static void AssertMetadata(MetadataModel model)
        {
            Assert.NotNull(model.MetadataType);
            Assert.Equal(3, model.MetadataKeyValuePairs.Count);
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.NotNull(model.Entry.Dc);
                Assert.Equal(default, model.DateAdded);
                Assert.Null(model.DcCredentials);
                Assert.Null(model.DcNetworkInformation);
            }
        }
    }

    [Theory]
    [InlineData("/forest?$top=1&$select=DomainControllers/Entry/Dc, ForestName&$expand=DomainControllers/Entry/Dc&$orderby=ForestName desc")]
    [InlineData("/forest?$top=1&$select=DomainControllers/Entry($select=Dc), ForestName&$expand=DomainControllers/Entry/Dc&$orderby=ForestName desc")]
    public async Task ForestTopWithSelectAndExpandDc_BothSyntaxes_DcShouldBeExpanded_RootComplexTypesShouldNotBeExpanded(string query)
    {
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);

            ForestModel model = collection.First();

            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.Id);
            Assert.Equal("Zulauf Forest", model.ForestName);
            Assert.Null(model.ForestWideCredentials);

            AssertDomainControllerEntry(model.DomainControllers);
            AssertDomainController(model.DomainControllers.Select(m => m.Entry.Dc));
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.Equal(2, models.Count);
            foreach (var model in models)
            {
                Assert.NotNull(model.Entry.Dc);
                Assert.Equal(default, model.DateAdded);
                Assert.Null(model.DcCredentials);
                Assert.Null(model.DcNetworkInformation);
            }
        }

        static void AssertDomainController(IEnumerable<DomainControllerModel> models)
        {
            foreach (var model in models)
            {
                Assert.NotEqual(default, model.Id);
                Assert.NotEqual(default, model.ForestId);
                Assert.NotNull(model.FullyQualifiedDomainName);
                AssertMetadata(model.Metadata);
                AssertAttributes(model.Attributes);
                Assert.Empty(model.Backups);
                Assert.NotEmpty(model.FsmoRoles);
            }
        }

        static void AssertAttributes(ICollection<ObjectAttributeModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.NotNull(model.Name);
                Assert.NotNull(model.Value);
            }
        }

        static void AssertMetadata(MetadataModel model)
        {
            Assert.NotNull(model.MetadataType);
            Assert.Equal(3, model.MetadataKeyValuePairs.Count);
        }
    }

    [Fact]
    public async void ForestSelectForestNameExpandDcSelectFullyQualifiedDomainName_DcShouldBeExpanded_ShouldOnlyReturnDcWithSelectedProperty()
    {
        string query = "/forest?$top=1&$select=ForestName&$expand=DomainControllers/Entry/Dc($select=FullyQualifiedDomainName)&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);

            ForestModel model = collection.First();

            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.Id);
            Assert.Equal("Zulauf Forest", model.ForestName);
            Assert.Null(model.ForestWideCredentials);

            AssertDomainControllerEntry(model.DomainControllers);
            AssertDomainController(model.DomainControllers.Select(m => m.Entry.Dc));
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.Equal(2, models.Count);
            foreach (var model in models)
            {
                Assert.NotNull(model.Entry.Dc);
                Assert.Equal(default, model.DateAdded);
                Assert.Null(model.DcCredentials);
                Assert.Null(model.DcNetworkInformation);
            }
        }

        static void AssertDomainController(IEnumerable<DomainControllerModel> models)
        {
            foreach (var model in models)
            {
                Assert.Equal(default, model.Id);
                Assert.Equal(default, model.ForestId);
                Assert.NotNull(model.FullyQualifiedDomainName);
                Assert.Null(model.Metadata);
                Assert.Empty(model.Attributes);
                Assert.Empty(model.Backups);
                Assert.Empty(model.FsmoRoles);
            }
        }
    }

    [Theory]
    [InlineData("/forest?$top=1&$select=ForestName, DomainControllers, DomainControllers/Entry/Dc&$expand=DomainControllers/Entry/Dc($select=FsmoRoles;$expand=Backups)&$orderby=ForestName asc")]
    [InlineData("/forest?$top=1&$select=ForestName, DomainControllers($select=DateAdded, Entry/Dc, DcCredentials, DcNetworkInformation)&$expand=DomainControllers/Entry/Dc($select=FsmoRoles;$expand=Backups)&$orderby=ForestName asc")]
    public async Task ForestSelectForestNameDomainControllersAndDc_BothSyntaxes_ExpandDcAndBackups_DcAndBackupShouldBeExpanded(string query)
    {        
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);

            ForestModel model = collection.First();
            AssertForestModel(model);
            AssertDomainControllerEntry(model.DomainControllers);
            AssertDomainController(model.DomainControllers.Select(m => m.Entry.Dc));
            AssertBackup(model.DomainControllers.SelectMany(m => m.Entry.Dc.Backups));
        }

        static void AssertForestModel(ForestModel model)
        {
            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.Id);
            Assert.Empty(model.Values);
            Assert.Equal("Abernathy Forest", model.ForestName);
            Assert.Null(model.ForestWideCredentials);
            Assert.Null(model.Metadata);                        
        }

        static void AssertBackup(IEnumerable<BackupModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.NotEqual(default, model.Id);
                Assert.NotEqual(default, model.ForestId);
                Assert.NotEqual(default, model.DateCreated);
                Assert.NotNull(model.Location);
                AssertCredentials(model.Location.Credentials);
                AssertNetworkInfo(model.Location.NetworkInformation);
            }
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.NotNull(model.Entry.Dc);
                Assert.NotEqual(default, model.DateAdded);
                AssertCredentials(model.DcCredentials);
                AssertNetworkInfo(model.DcNetworkInformation);
            }
        }

        static void AssertDomainController(IEnumerable<DomainControllerModel> models)
        {
            Assert.NotEmpty(models);
            foreach (var model in models)
            {
                Assert.Equal(default, model.Id);
                Assert.Equal(default, model.ForestId);
                Assert.Null(model.FullyQualifiedDomainName);
                Assert.Null(model.Metadata);
                Assert.Empty(model.Attributes);
                Assert.NotEmpty(model.FsmoRoles);
            }
        }

        static void AssertNetworkInfo(params NetworkInformationModel?[] models)
        {
            foreach (var model in models)
            {
                Assert.NotNull(model);
                Assert.NotNull(model.Address);
            }
        }

        static void AssertCredentials(params CredentialsModel?[] models)
        {
            foreach (var model in models)
            {
                Assert.NotNull(model);
                Assert.NotNull(model.Username);
                Assert.NotNull(model.Password);
            }
        }
    }

    [Theory]
    [InlineData("/forest?$select=Metadata($select=MetadataKeyValuePairs($select=Value))")]
    [InlineData("/forest?$select=Metadata/MetadataKeyValuePairs/Value")]
    public async Task ForestSelectComplexProperties_BothSyntaxes_ShouldOnlyReturnComplexPropertiesWithSelectedProperties(string query)
    {
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection)
            {
                AssertModel(model);
            }
        }

        static void AssertModel(ForestModel model)
        {
            Assert.NotNull(model.Metadata);
            Assert.Null(model.Metadata.MetadataType);
            Assert.Equal(3, model.Metadata.MetadataKeyValuePairs.Count);
            Assert.All(model.Metadata.MetadataKeyValuePairs, pair => Assert.Null(pair.Key));
            Assert.All(model.Metadata.MetadataKeyValuePairs, pair => Assert.NotEqual(default, pair.Value));
        }
    }

    [Theory]
    [InlineData("/forest?$select=Metadata($select=MetadataType, MetadataKeyValuePairs)")]
    [InlineData("/forest?$select=Metadata/MetadataType, Metadata/MetadataKeyValuePairs")]
    [InlineData("/forest?$select=Metadata($select=MetadataType, MetadataKeyValuePairs($select=Key, Value))")]
    [InlineData("/forest?$select=Metadata/MetadataType, Metadata/MetadataKeyValuePairs/Key, Metadata/MetadataKeyValuePairs/Value")]
    public async Task ForestSelectComplexType_BothSyntaxes_ShouldReturnFullyPopulatedComplexTypes(string query)
    {
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection)
            {
                AssertModel(model.Metadata);
            }
        }

        static void AssertModel(MetadataModel model)
        {
            Assert.NotNull(model);
            Assert.NotNull(model.MetadataType);
            Assert.Equal(3, model.MetadataKeyValuePairs.Count);
            Assert.All(model.MetadataKeyValuePairs, pair => Assert.NotNull(pair.Key));
            Assert.All(model.MetadataKeyValuePairs, pair => Assert.Matches(@"^Key\d$", pair.Key));
            Assert.All(model.MetadataKeyValuePairs, pair => Assert.NotEqual(default, pair.Value));
        }
    }

    [Theory]
    [InlineData("/forest?$expand=DomainControllers/Entry/Dc($expand=Backups($select=Location/Credentials/Username))")]
    [InlineData("/forest?$expand=DomainControllers/Entry/Dc($expand=Backups($select=Location($select=Credentials($select=Username))))")]
    public async Task ForestExpandDcAndBackupSelectComplexProperties_BothSyntaxes_ShouldOnlyReturnComplexPropertiesWithSelectedProperties(string query)
    {
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection.SelectMany(m => m.DomainControllers.SelectMany(m => m.Entry.Dc.Backups)))
            {
                AssertModel(model);
            }
        }

        static void AssertModel(BackupModel model)
        {
            Assert.Equal(default, model.Id);
            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.DateCreated);
            Assert.NotNull(model.Location);
            Assert.NotNull(model.Location.Credentials);
            Assert.NotNull(model.Location.Credentials.Username);
            Assert.Null(model.Location.Credentials.Password);
            Assert.Null(model.Location.NetworkInformation);
        }
    }

    [Fact]
    public async Task ForestExpandDcAndBackupSelectComplexProperty_ShouldReturnFullyPopulatedComplexProperty()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($expand=Backups($select=Location))";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection.SelectMany(m => m.DomainControllers.SelectMany(m => m.Entry.Dc.Backups)))
            {
                AssertModel(model);
            }
        }

        static void AssertModel(BackupModel model)
        {
            Assert.Equal(default, model.Id);
            Assert.Equal(default, model.ForestId);
            Assert.Equal(default, model.DateCreated);
            Assert.NotNull(model.Location);
            Assert.NotNull(model.Location.Credentials);
            Assert.NotNull(model.Location.Credentials.Username);
            Assert.NotNull(model.Location.Credentials.Password);
            Assert.NotNull(model.Location.NetworkInformation);
            Assert.NotNull(model.Location.NetworkInformation.Address);
        }
    }

    private Task<ICollection<TModel>> GetUsingCustomNameSpace<TModel, TData>(string query,
            ODataQueryOptions<TModel>? options = null, QuerySettings? querySettings = null) 
        where TModel : class 
        where TData : class
    {
        return GetAsync<TModel, TData>(query, options, querySettings, "com.FooBar");
    }

    private ICollection<TModel> Get<TModel, TData>(string query, ODataQueryOptions<TModel>? options = null) 
        where TModel : class 
        where TData : class
    {
        return
        (
            DoGet
            (
                this.dbContainer.GetContainer().GetItemLinqQueryable<TData>(allowSynchronousQueryExecution: true).AsQueryable(),
                serviceProvider.GetRequiredService<IMapper>()
            )
        ).ToList();

        ICollection<TModel> DoGet(IQueryable<TData> dataQueryable, IMapper mapper)
        {
            return dataQueryable.Get
            (
                mapper,
                options ?? GetODataQueryOptions<TModel>(query),
                new QuerySettings { ODataSettings = new ODataSettings { HandleNullPropagation = HandleNullPropagationOption.False } }
            );
        }
    }


    private async Task<ICollection<TModel>> GetAsync<TModel, TData>(
        string query, ODataQueryOptions<TModel>? options = null, QuerySettings? querySettings = null, string? customNamespace = null) 
        where TModel : class 
        where TData : class
    {
        return
        (
            await DoGet
            (
                this.dbContainer.GetContainer().GetItemLinqQueryable<TData>().AsQueryable(),
                serviceProvider.GetRequiredService<IMapper>()
            )
        ).ToList();

        async Task<ICollection<TModel>> DoGet(IQueryable<TData> dataQueryable, IMapper mapper)
        {
            return await dataQueryable.GetAsync
            (
                mapper, 
                options ?? GetODataQueryOptions<TModel>(query, customNamespace), 
                querySettings!
            );
        }
    }

    private ODataQueryOptions<TModel> GetODataQueryOptions<TModel>(string query, string? customNamespace = null) 
        where TModel : class
    {
        return ODataHelpers.GetODataQueryOptions<TModel>
        (
            query,
            serviceProvider,
            customNamespace
        );
    }
}
