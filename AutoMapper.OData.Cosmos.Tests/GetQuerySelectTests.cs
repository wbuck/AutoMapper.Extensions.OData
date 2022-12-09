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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
    public async Task FetchAllForestDocumentsWithNoSelectsOrFilters()
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

    [Fact]
	public async Task ForestSelectForestNameExpandDomainControllersOrderByForestNameAscending_DcShouldBeExpanded_ComplexTypesShouldBeExpanded()
	{        
        const string query = "/forest?$select=ForestName&$expand=DomainControllers/Dc&$orderby=ForestName";

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
            Assert.NotEmpty(model.DomainControllers);
            Assert.All(model.DomainControllers.Select(entry => entry.DcCredentials), creds => Assert.NotNull(creds));
            Assert.All(model.DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.NotNull(loc));
            Assert.All(model.DomainControllers.Select(entry => entry.Dc), dc => Assert.NotNull(dc));
            Assert.All(model.DomainControllers.Select(entry => entry.Dc.FsmoRoles), roles => Assert.NotEmpty(roles));
            Assert.All(model.DomainControllers.Select(entry => entry.Dc.Backups), backups => Assert.Empty(backups));
            Assert.All(model.DomainControllers.Select(entry => entry.Dc.Attributes), attributes => Assert.NotEmpty(attributes));
            Assert.Equal(forestName, model.ForestName);
            Assert.Null(model.ForestWideCredentials);
        }
	}

    [Fact]
    public async Task ForestTopWithSelectAndExpandDomainControllersFilterEqAndOrderByForestName_DcShouldBeExpanded_ComplexTypesShouldBeExpanded_ShouldReturnSingleForest()
    {
        const string query = "/forest?$top=5&$select=DomainControllers/Dc&$expand=DomainControllers/Dc&$filter=ForestName eq 'Rolfson Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(default, collection.First().ForestId);
            Assert.Equal(default, collection.First().Id);
            Assert.Null(collection.First().ForestName);
            Assert.Null(collection.First().ForestWideCredentials);
            Assert.Equal(4, collection.First().DomainControllers.Count);            
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcCredentials), creds => Assert.NotNull(creds));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.NotNull(loc));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.Equal("http://www.rolfson.com/", loc!.Address));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc), dc => Assert.NotNull(dc));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.FsmoRoles), roles => Assert.NotEmpty(roles));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.Backups), backups => Assert.Empty(backups));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.Attributes), attributes => Assert.NotEmpty(attributes));
        }
    }

    [Fact]
    public async void TopWithSelectAndFilterForestNameExpandDomainControllersSelectFullyQualifiedDomainName()
    {
        string query = "/forest?$top=5&$select=ForestName&$expand=DomainControllers/Dc($select=FullyQualifiedDomainName)&$filter=ForestName eq 'Zulauf Forest'";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(default, collection.First().ForestId);
            Assert.Equal(default, collection.First().Id);
            Assert.Equal("Zulauf Forest", collection.First().ForestName);
            Assert.Null(collection.First().ForestWideCredentials);
            Assert.Equal(2, collection.First().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcCredentials), creds => Assert.NotNull(creds));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.NotNull(loc));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.DcNetworkInformation), loc => Assert.Equal("http://zulauf.net/", loc!.Address));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc), dc => Assert.NotNull(dc));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.FullyQualifiedDomainName), name => Assert.NotNull(name));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.FsmoRoles), roles => Assert.Empty(roles));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.Backups), backups => Assert.Empty(backups));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.Attributes), attributes => Assert.Empty(attributes));
        }
    }

    [Fact]
    public async Task TopWithSelectAndExpandDomainControllersExpandBackupsFilterAndOrderByForestName()
    {
        const string query = "/forest?$select=ForestName, DomainControllers/Dc&$expand=DomainControllers/Dc($select=FsmoRoles&$expand=Backups)&$filter=contains(ForestName, 'Abernathy Forest')";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.FsmoRoles), roles => Assert.NotEmpty(roles));
            Assert.Equal(7, collection.First().DomainControllers.SelectMany(entry => entry.Dc.Backups).Count());
            Assert.All(collection.First().DomainControllers.SelectMany(entry => entry.Dc.Backups), backup => Assert.NotNull(backup.Location));
            Assert.All(collection.First().DomainControllers.SelectMany(entry => entry.Dc.Backups), backup => Assert.NotNull(backup.Location.Credentials));
            Assert.All(collection.First().DomainControllers.SelectMany(entry => entry.Dc.Backups), backup => Assert.NotNull(backup.Location.NetworkInformation));
            Assert.All(collection.First().DomainControllers.SelectMany(entry => entry.Dc.Backups), backup => Assert.Equal("admin@abernathy.com", backup.Location.Credentials!.Username));
            Assert.All(collection.First().DomainControllers.Select(entry => entry.Dc.Attributes), attributes => Assert.Empty(attributes));
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
