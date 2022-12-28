using AutoMapper.OData.Cosmos.Tests.Binder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace AutoMapper.OData.Cosmos.Tests;

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

        var oDataOptions = new ODataOptions().AddRouteComponents("key", model,
            x => x.AddSingleton<ISearchBinder, ForestSearchBinder>());

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
