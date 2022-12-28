using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;

namespace AutoMapper.OData.Cosmos.Tests;

[Collection(nameof(CosmosContainer))]
public sealed class GetQueryTests
{
    private readonly CosmosContainer dbContainer;
    private readonly IServiceProvider serviceProvider;

    public GetQueryTests(CosmosContainer dbContainer)
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
    public async Task ForestModelSearch()
    {
        const string query = "/forest?$search=\"Rolfson Forest\"";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal("Rolfson Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelSearchAndFilter()
    {
        const string query = "/forest?$search=\"Rolfson Forest\"&$filter=ForestName eq 'Zulauf Forest'";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(0, collection.Count);
        }
    }

    [Fact]
    public async void ForestModelCreatedOnFilterServerUTCTimeZone()
    {
        var querySettings = new QuerySettings
        {
            ODataSettings = new ODataSettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                TimeZone = TimeZoneInfo.Utc
            }
        };

        string query = "/forest?$filter=CreatedDate eq 2022-12-25T00:00:00Z";
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        query = "/forest?$filter=CreatedDate eq 2022-12-24T19:00:00-05:00";
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
        }
    }

    [Fact]
    public async void ForestModelCreatedOnFilterServerESTTimeZone()
    {
        var querySettings = new QuerySettings
        {
            ODataSettings = new ODataSettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
            }
        };

        string query = "/forest?$filter=CreatedDate eq 2022-12-25T05:00:00Z";
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        query = "/forest?$filter=CreatedDate eq 2022-12-25T00:00:00-05:00";
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
        }
    }

    [Fact]
    public async void ForestModelExpandBuildingsFilterEqAndOrderBy()
    {
        string query = "/ForestModel?$top=5&$expand=DomainControllers/Dc&$filter=ForestName eq 'Rolfson Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Dc), dc => Assert.NotNull(dc));
            Assert.Equal("Rolfson Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestSelectValues_NestedFilter_ShouldReturnFilteredLiteralCollectionOfValues()
    {
        const string query = "/forest?$select=Values($filter=$this gt 1 and $this lt 101)&$orderby=ForestName";
        List<List<int>> expectedValues = new()
        {
            new() { 100 },
            new() { 42, 2 },
            new() { 2 },
        };

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));       

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            foreach (var (model, expected) in collection.Zip(expectedValues))
            {
                AssertModel(model, expected);
            }
        }

        static void AssertModel(ForestModel model, List<int> expected)
        {
            Assert.Equal(expected, model.Values);
        }
    }

    private Task<ICollection<TModel>> GetUsingCustomNameSpace<TModel, TData>(string query,
            ODataQueryOptions<TModel>? options = null, QuerySettings? querySettings = null)
        where TModel : class
        where TData : class
    {
        return GetAsync<TModel, TData>(query, options, querySettings, "com.FooBar");
    }

    private ICollection<TModel> Get<TModel, TData>(string query, ODataQueryOptions<TModel>? options = null, QuerySettings? querySettings = null)
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
                querySettings!
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
