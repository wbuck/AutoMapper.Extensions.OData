using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
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
                Assert.Null(model.Dc);
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
        const string query = "/forest?$select=ForestName&$expand=DomainControllers/Dc&$orderby=ForestName desc";

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
            AssertDomainController(model.DomainControllers.Select(m => m.Dc));
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
                Assert.NotNull(model.Dc);
                Assert.Equal(default, model.DateAdded);
                Assert.Null(model.DcCredentials);
                Assert.Null(model.DcNetworkInformation);
            }
        }
    }

    [Theory]
    [InlineData("/forest?$top=1&$select=DomainControllers/Dc, ForestName&$expand=DomainControllers/Dc&$orderby=ForestName desc")]
    [InlineData("/forest?$top=1&$select=DomainControllers($select=Dc), ForestName&$expand=DomainControllers/Dc&$orderby=ForestName desc")]
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
            AssertDomainController(model.DomainControllers.Select(m => m.Dc));
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.Equal(2, models.Count);
            foreach (var model in models)
            {
                Assert.NotNull(model.Dc);
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
        string query = "/forest?$top=1&$select=ForestName&$expand=DomainControllers/Dc($select=FullyQualifiedDomainName)&$orderby=ForestName desc";
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
            AssertDomainController(model.DomainControllers.Select(m => m.Dc));
        }

        static void AssertDomainControllerEntry(ICollection<DomainControllerEntryModel> models)
        {
            Assert.Equal(2, models.Count);
            foreach (var model in models)
            {
                Assert.NotNull(model.Dc);
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
    [InlineData("/forest?$top=1&$select=ForestName, DomainControllers, DomainControllers/Dc&$expand=DomainControllers/Dc($select=FsmoRoles;$expand=Backups)&$orderby=ForestName asc")]
    [InlineData("/forest?$top=1&$select=ForestName, DomainControllers($select=DateAdded, Dc, DcCredentials, DcNetworkInformation)&$expand=DomainControllers/Dc($select=FsmoRoles;$expand=Backups)&$orderby=ForestName asc")]
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
            AssertDomainController(model.DomainControllers.Select(m => m.Dc));
            AssertBackup(model.DomainControllers.SelectMany(m => m.Dc.Backups));
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
                Assert.NotNull(model.Dc);
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
    [InlineData("/forest?$expand=DomainControllers/Dc($expand=Backups($select=Location/Credentials/Username))")]
    [InlineData("/forest?$expand=DomainControllers/Dc($expand=Backups($select=Location($select=Credentials($select=Username))))")]
    public async Task ForestExpandDcAndBackupSelectComplexProperties_BothSyntaxes_ShouldOnlyReturnComplexPropertiesWithSelectedProperties(string query)
    {
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection.SelectMany(m => m.DomainControllers.SelectMany(m => m.Dc.Backups)))
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
        const string query = "/forest?$expand=DomainControllers/Dc($expand=Backups($select=Location))";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var model in collection.SelectMany(m => m.DomainControllers.SelectMany(m => m.Dc.Backups)))
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

    [Fact]
    public async Task ForestSelectValues_ShouldReturnLiteralCollectionOfValues()
    {
        const string query = "/forest?$select=Values($filter=$this gt 500 and $this lt 1000)";        
        //const string query = "/forest?$select=DomainControllers($filter=$this/DcCredentials/Username eq 'administrator1')";
        //const string query = "/forest?$expand=DomainControllers/Dc($expand=Backups($filter=ForestId eq 00000000-0000-0000-0000-000000000000))";
        //const string query = "/forest?$select=Values($orderby=$this)";
        //const string query = "/forest?$expand=DomainControllers/Dc($expand=Backups)";
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
            Assert.Equal(default, model.Id);
            Assert.Equal(default, model.ForestId);
            Assert.Null(model.ForestName);
            Assert.Null(model.ForestWideCredentials);
            Assert.Null(model.Metadata);
            Assert.Empty(model.DomainControllers);
            Assert.Equal(3, model.Values.Count);
        }
    }

    [Fact]
    public async Task TBD()
    {
        //const string query = "/forest?$select=ForestWideCredentials/Username, ForestWideCredentials/Password";
        //const string query = "/forest?$select=ForestWideCredentials($select=Username), DomainControllers/Dc&$expand=DomainControllers/Dc($select=FullyQualifiedDomainName, Fake/FakeInternal/Age)";
        //const string query = "/forest?$select=Fake($select=FakeInternal($select=Name, Age)), ForestName, DomainControllers($select=Dc, DateAdded, DcCredentials($select=Username, Password))&$expand=DomainControllers/Dc($select=Backups, FullyQualifiedDomainName;$expand=Backups($select=Location/Credentials/Username))";

        const string query = "/forest?$select=Fake($select=FakeInternal($select=Name, Age)), ForestName, DomainControllers($select=Dc, DateAdded, DcCredentials)&$expand=DomainControllers/Dc($select=Backups, FullyQualifiedDomainName, Attributes, FsmoRoles;$expand=Backups($select=Location))";
        //const string query = "/forest?$select=Fake/FakeInternal/Name, ForestName, DomainControllers($select=Dc, DateAdded, DcCredentials)&$expand=DomainControllers/Dc($select=Backups, FullyQualifiedDomainName;$expand=Backups($select=Location))";

        //const string query = "/forest?$select=Fake/FakeInternal/Name, Fake/FakeInternal/Age, ForestName, DomainControllers/Dc, DomainControllers/DateAdded, DomainControllers/DcCredentials/Username, DomainControllers/DcCredentials/Password&expand=DomainControllers/Dc($select=Backups, FullyQualifiedDomainName;$expand=Backups($select=Location/Credentials/Username))";

        // fake
        //const string query = "/forest?$select=Fake/FakeInternal/Name, Fake/FakeInternal/Age, ForestName, DomainControllers/Dc, DomainControllers/DateAdded, DomainControllers/DcCredentials/Username, DomainControllers/DcCredentials/Password&expand=DomainControllers/Dc($select=Complex/Complex2/Backups, Complex/Complex2/Username, FullyQualifiedDomainName;$expand=Complex/Complex2/Backups($select=Location/Credentials/Username))";

        //const string query = "/forest?$select=Fake/FakeInternal/Name, Fake/FakeInternal/Age, ForestName, Values($filter=$it lt 10)";
        //const string query = "/forest?$select=DomainControllers($filter=DcCredentials/Username eq 'administrator')";
        //const string query = "/forest?$select=ForestWideCredentials/Username, ForestWideCredentials/Password";
        //const string query = "/forest?$select=Fake/FakeInternal/Name, Fake/FakeInternal/Age, DomainControllers/Dc";

        //const string query = "/forest?$expand=DomainControllers/Dc($select=Fake($select=FakeInternal($select=Name)), Backups;$expand=Backups)";
        //const string query = "/forest?$expand=DomainControllers/Dc";

        //const string query = "/forest?$select=Fake($select=FakeInternal($select=Name, Age))";
        //const string query = "/forest?$select=Fake/FakeInternal/Name, Fake/FakeInternal/Age";

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
            Assert.NotEqual(default, model.ForestId);
            Assert.NotEqual(default, model.Id);
            Assert.NotEmpty(model.DomainControllers);
            Assert.All(model.DomainControllers.Select(entry => entry.DcCredentials), creds => Assert.NotNull(creds));
            Assert.All(model.DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.NotNull(loc));
            Assert.All(model.DomainControllers.Select(entry => entry.Dc), dc => Assert.Null(dc));
            Assert.Equal(forestName, model.ForestName);
            Assert.NotNull(model.ForestWideCredentials);
            Assert.NotNull(model.ForestWideCredentials.Username);
            Assert.NotNull(model.ForestWideCredentials.Password);
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

public static class ODataHelpers
{
    private const string BaseAddress = "http://localhost:16324";

    public static ODataQueryOptions<T> GetODataQueryOptions<T>(string queryString, IServiceProvider serviceProvider, string? customNamespace = null) 
        where T : class
    {
        var builder = new ODataConventionModelBuilder();

        if (customNamespace is not null)
            builder.Namespace = customNamespace;

        builder.EntitySet<T>(typeof(T).Name);

        IEdmModel model = builder.GetEdmModel();
        IEdmEntitySet entitySet = model.EntityContainer.FindEntitySet(typeof(T).Name);
        ODataPath path = new(new EntitySetSegment(entitySet));

        var request = new DefaultHttpContext()
        {
            RequestServices = serviceProvider
        }.Request;

        //var oDataOptions = new ODataOptions().AddRouteComponents("key", model,
        //    x => x.AddSingleton<ISearchBinder, OpsTenantSearchBinder>());
        var oDataOptions = new ODataOptions().AddRouteComponents("key", model);
        var (_, routeProvider) = oDataOptions.RouteComponents["key"];

        request.ODataFeature().Services = routeProvider;
        var oDataQueryOptions = new ODataQueryOptions<T>
        (
            new ODataQueryContext(model, typeof(T), path),
            BuildRequest(request, new Uri(BaseAddress + queryString))
        );
        return oDataQueryOptions;

        static HttpRequest BuildRequest(HttpRequest request, Uri uri)
        {
            request.Method = "GET";
            request.Host = new HostString(uri.Host, uri.Port);
            request.Path = uri.LocalPath;
            request.QueryString = new QueryString(uri.Query);

            return request;
        }

    }    
}
