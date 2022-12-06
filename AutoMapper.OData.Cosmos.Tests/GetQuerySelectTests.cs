using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Extensions;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

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
	public async Task FirstTest()
	{
        //&$expand=Buildings&$orderby=Name
        //const string query = "/forest?$expand=AdObjects/Dc&$orderby=ForestName";
        //const string query = "/forest?$select=ForestName, ForestId, FakeType&$expand=AdObjects/Dc&$orderby=ForestName";
        // string query = "/forest?$select=ForestName&$expand=AdObjects($expand=Dc($expand=Attributes))&$orderby=ForestName";
        //const string query = "/forest?$expand=AdObjects/Dc&$orderby=ForestName";
        const string query = "/forest?$expand=AdObjects/Dc&$orderby=ForestName";
        var queryable = this.dbContainer.GetContainer().GetItemLinqQueryable<Forest>().AsQueryable();

        //var list = new List<Forest>().AsQueryable();
        // Forest -> AdObject -> ObjectAttribute
       // var q = list.Select(v => v.AdObjects.Select(a => a.Dc.Attributes.Select(c => c.Value)));
       

        //Test(Get<OpsTenant, TMandator>(query));
        Test(await GetAsync<ForestModel, Forest>(query, queryable));
        //Test(await GetUsingCustomNameSpace<OpsTenant, TMandator>(query));

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            //Assert.Equal(2, collection.First().Buildings.Count);
            //Assert.Equal("One", collection.First().Name);
            //Assert.Equal(default, collection.First().Identity);
        }

        //var query = this.dbContainer.GetContainer().GetItemLinqQueryable<Forest>()
		//	.Where(f => f.Name.Contains("Forest1", StringComparison.OrdinalIgnoreCase));
        //
		//using var it = query.ToFeedIterator();
		//var response = await it.ReadNextAsync().ConfigureAwait(false);
		//Assert.NotNull(response);
	}

    private async Task<ICollection<TModel>> GetAsync<TModel, TData>(
        string query, IQueryable<TData> dataQueryable, ODataQueryOptions<TModel>? options = null, QuerySettings? querySettings = null, string? customNamespace = null) 
        where TModel : class 
        where TData : class
    {
        return
        (
            await DoGet
            (
                serviceProvider.GetRequiredService<IMapper>()
            )
        ).ToList();

        async Task<ICollection<TModel>> DoGet(IMapper mapper)
        {
            return await dataQueryable.GetAsync
            (
                mapper, 
                options ?? GetODataQueryOptions<TModel>(query, customNamespace), 
                querySettings
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

    private static void ConfigureDcEntitySet(ODataConventionModelBuilder builder) =>
        builder.EntitySet<DomainControllerModel>(nameof(DomainControllerModel), config =>
        {
            config.CollectionProperty(e => e.FsmoRoles);
            config.Expand(SelectExpandType.Automatic, nameof(DomainControllerModel.FsmoRoles));
        });

    private static void ConfigureAdObjectComplexType(ODataConventionModelBuilder builder) =>
        builder.EntitySet<DomainControllerModel>(nameof(DomainControllerModel), config =>
        {
            config.Expand(SelectExpandType.Automatic, nameof(DomainControllerModel.FsmoRoles));
        });

    public static ODataQueryOptions<T> GetODataQueryOptions<T>(string queryString, IServiceProvider serviceProvider, string? customNamespace = null) 
        where T : class
    {
        var builder = new ODataConventionModelBuilder();

        if (customNamespace is not null)
            builder.Namespace = customNamespace;

        builder.EntitySet<T>(typeof(T).Name);
        ConfigureDcEntitySet(builder);

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
