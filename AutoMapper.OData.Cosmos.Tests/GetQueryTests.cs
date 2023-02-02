using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System.Text.Json;

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

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(0, collection.Count);
        }
    }

    [Fact]
    public async Task ForestModelCreatedOnFilterServerUTCTimeZone()
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
    public async Task ForestModelCreatedOnFilterServerESTTimeZone()
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
    public async Task ForestModelExpandDcFilterEqAndOrderBy()
    {
        string query = "/forest?$top=5&$expand=DomainControllers/Entry/Dc&$filter=ForestName eq 'Rolfson Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.Equal("Rolfson Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelExpandDcFilterNeAndOrderBy()
    {
        const string query = "/forest?$top=5&$expand=DomainControllers/Entry/Dc&$filter=ForestName ne 'Zulauf Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.Equal(4, collection.Last().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.All(collection.Last().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.Equal("Rolfson Forest", collection.First().ForestName);
            Assert.Equal("Abernathy Forest", collection.Last().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelFilterEqNoExpand()
    {
        const string query = "/forest?$filter=ForestName eq 'Abernathy Forest'";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.Null(dc));
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelFilterGtDateNoExpand()
    {
        string query = "/forest?$filter=CreatedDate gt 2022-12-26T12:00:00.00Z";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal("Zulauf Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelFilterLtDateNoExpand()
    {
        string query = "/forest?$filter=CreatedDate lt 2022-12-26T12:00:00.00Z&$orderby=ForestName";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
            Assert.Equal("Rolfson Forest", collection.Last().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelExpandDcNoFilterAndOrderBy()
    {
        string query = "/forest?$top=2&$expand=DomainControllers/Entry/Dc&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal(2, collection.First().DomainControllers.Count);
            Assert.Equal(4, collection.Last().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.All(collection.Last().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.Equal("Zulauf Forest", collection.First().ForestName);
            Assert.Equal("Rolfson Forest", collection.Last().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelNoExpandNoFilterAndOrderBy()
    {
        string query = "/forest?$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(new List<string> { "Zulauf Forest", "Rolfson Forest", "Abernathy Forest" }, collection.Select(m => m.ForestName));
        }
    }

    [Fact]
    public async Task ForestModelNoExpandFilterEqAndOrderBy()
    {
        string query = "/forest?$top=5&$filter=ForestName eq 'Rolfson Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.Single().DomainControllers.Count);
            Assert.All(collection.Single().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.Null(dc));
            Assert.Equal("Rolfson Forest", collection.First().ForestName);
        }
    }

    [Fact]
    public async Task ForestModelExpandDcSelectForestNameAndBackupExpandBackupFilterNeAndOrderBy()
    {
        string query = "/forest?$top=5&$select=ForestName&$expand=DomainControllers/Entry/Dc($select=FullyQualifiedDomainName,Backups;$expand=Backups($select=ForestId, Location))&$filter=ForestName ne 'Zulauf Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal(new[] { "Rolfson Forest", "Abernathy Forest" }, collection.Select(m => m.ForestName));

            var dcs = collection.SelectMany(m => m.DomainControllers.Select(m => m.Entry.Dc)).ToList();
            Assert.All(dcs.Select(m => m.FullyQualifiedDomainName), fqdn => Assert.NotNull(fqdn));
            Assert.All(dcs.SelectMany(m => m.Backups), backup => Assert.NotNull(backup));

            var backups = dcs.SelectMany(dc => dc.Backups).ToList();
            Assert.All(backups.Select(m => m.ForestId), id => Assert.NotEqual(default, id));
            Assert.All(backups.Select(m => m.Location), location => Assert.NotNull(location));
            Assert.All(backups.Select(m => m.Location.Credentials), creds => 
            {
                Assert.NotNull(creds);
                Assert.NotNull(creds.Username);
                Assert.NotNull(creds.Password);
            });
            Assert.All(backups.Select(m => m.Location.NetworkInformation), info =>
            {
                Assert.NotNull(info);
                Assert.NotNull(info.Address);                
            });
        }
    }

    [Fact]
    public async Task ForestModelExpandDcExpandBackupFilterNeAndOrderBy()
    {
        const string query = "/forest?$top=5&$expand=DomainControllers/Entry/Dc($expand=Backups)&$filter=ForestName ne 'Abernathy Forest'&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal(new[] { "Zulauf Forest", "Rolfson Forest" }, collection.Select(m => m.ForestName));

            var dcs = collection.SelectMany(m => m.DomainControllers.Select(m => m.Entry.Dc)).ToList();
            Assert.All(dcs.SelectMany(m => m.Backups), backup => Assert.NotNull(backup));

            var backups = dcs.SelectMany(dc => dc.Backups).ToList();
            Assert.All(backups.Select(m => m.Location), location => Assert.NotNull(location));
            Assert.All(backups.Select(m => m.Location.Credentials), creds =>
            {
                Assert.NotNull(creds);
                Assert.NotNull(creds.Username);
                Assert.NotNull(creds.Password);
            });
            Assert.All(backups.Select(m => m.Location.NetworkInformation), info =>
            {
                Assert.NotNull(info);
                Assert.NotNull(info.Address);
            });
        }
    }

    [Fact]
    public async Task ForestModelExpandDcFilterDcPropertyEqAndOrderBy()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc&$filter=DomainControllers/any(entry: entry/Entry/Dc/FullyQualifiedDomainName eq 'dc1.abernathy.com')&$orderby=ForestName desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);           
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.NotNull(dc));
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
            Assert.Equal("dc1.abernathy.com", collection.First().DomainControllers.First().Entry.Dc.FullyQualifiedDomainName);
        }
    }

    [Fact]
    public async Task ForestModelFilterComplexProperty()
    {
        const string query = "/forest?$filter=Metadata/MetadataType eq 'Abernathy Metadata'";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(1, collection.Count);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.All(collection.First().DomainControllers.Select(m => m.Entry.Dc), dc => Assert.Null(dc));
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
            Assert.Equal("Abernathy Metadata", collection.First().Metadata.MetadataType);
        }
    }

    [Fact]
    public async Task ForestModelOrderByComplexProperty()
    {
        const string query = "/forest?$orderby=Metadata/MetadataType desc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(new[] { "Zulauf Forest", "Rolfson Forest", "Abernathy Forest" }, collection.Select(m => m.ForestName));
        }
    }
    
    private sealed record Location
    {
        public int Start { get; init; }
        public int End { get; init; }
    }

    private sealed record Error
    {
        public string Severity { get; init; } = default!;
        public Location Location { get; init; } = default!;
        public string Code { get; init; } = default!;
        public string Message { get; init; } = default!;
    }

    private sealed record ErrorDetails
    {
        public Error[] Errors { get; init; } = Array.Empty<Error>();
    }

    private static ErrorDetails GetDetails(string jsonError) =>
        JsonSerializer.Deserialize<ErrorDetails>(jsonError, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true })!;

    [Fact]
    public async Task FilterPrimitiveCollectionWithTopInSubqueryShouldFailBecauseTopInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$select=Values($top=1)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Equal(2, details.Errors.Length);

            List<Error> errors = details.Errors.OrderBy(e => e.Message).ToList();
            Assert.Equal("'ORDER BY' is not supported in subqueries.", errors[0].Message);
            Assert.Equal("'TOP' is not supported in subqueries.", errors[1].Message);
        }
    }

    [Fact]
    public async Task OrderByAndThenByOnPrimitiveCollectionShouldFailBecauseOrderByInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$select=Values($orderby=$this,$this)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Single(details.Errors);

            Error error = details.Errors[0];
            Assert.Equal("'ORDER BY' is not supported in subqueries.", error.Message);
        }
    }

    [Fact]
    public async Task OrderByOnPrimitiveCollectionShouldFailBecauseOrderByInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$select=Values($orderby=$this)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Single(details.Errors);

            Error error = details.Errors[0];
            Assert.Equal("'ORDER BY' is not supported in subqueries.", error.Message);
        }
    }

    [Fact]
    public async Task FilterEntityCollectionWithTopAndSkipInSubqueryShouldFailBecauseTopAndSkipInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($top=1; $skip=2)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Equal(2, details.Errors.Length);

            List<Error> errors = details.Errors.OrderBy(e => e.Message).ToList();
            Assert.Equal("'OFFSET LIMIT' clause is not supported in subqueries.", errors[0].Message);
            Assert.Equal("'ORDER BY' is not supported in subqueries.", errors[1].Message);            
        }
    }

    [Fact]
    public async Task FilterEntityCollectionWithTopAndSkipInSubqueryWithOrderByShouldFailBecauseTopSkipAndOrderByInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($orderby=FullyQualifiedDomainName,Metadata/MetadataType;$top=1;$skip=2)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Equal(2, details.Errors.Length);

            List<Error> errors = details.Errors.OrderBy(e => e.Message).ToList();
            Assert.Equal("'OFFSET LIMIT' clause is not supported in subqueries.", errors[0].Message);
            Assert.Equal("'ORDER BY' is not supported in subqueries.", errors[1].Message);
        }
    }

    [Fact]
    public async Task FilterComplexCollectionWithTopAndSkipInSubqueryWithOrderByShouldFailBecauseTopSkipAndOrderByInSubqueryIsNotSupportedByCosmosDb()
    {
        const string query = "/forest?$select=DomainControllers($orderby=DateAdded;$top=1;$skip=2)";

        Test(Assert.Throws<CosmosException>(() => Get<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetAsync<ForestModel, Forest>(query)));
        Test(await Assert.ThrowsAsync<CosmosException>(() => GetUsingCustomNameSpace<ForestModel, Forest>(query)));

        static void Test(CosmosException ex)
        {
            ErrorDetails details = GetDetails(ex.InnerException!.Message);
            Assert.Equal(2, details.Errors.Length);

            List<Error> errors = details.Errors.OrderBy(e => e.Message).ToList();
            Assert.Equal("'OFFSET LIMIT' clause is not supported in subqueries.", errors[0].Message);
            Assert.Equal("'ORDER BY' is not supported in subqueries.", errors[1].Message);
        }
    }

    [Fact]
    public async Task ForestModelExpandDcExpandBackupOrderByForestNameSkipTakeWithCount()
    {
        string query = "/forest?$skip=2&$top=1&$expand=DomainControllers/Entry/Dc($expand=Backups)&$orderby=ForestName desc&$count=true";
        ODataQueryOptions<ForestModel> options = ODataHelpers.GetODataQueryOptions<ForestModel>
        (
            query,
            serviceProvider
        );
        Test(Get<ForestModel, Forest>(query, options));
        Test(await GetAsync<ForestModel, Forest>(query, options));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, options));

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, options.Request.ODataFeature().TotalCount);
            Assert.Equal(1, collection.Count);
            Assert.Equal("Abernathy Forest", collection.First().ForestName);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.Equal(7, collection.First().DomainControllers.Sum(dc => dc.Entry.Dc.Backups.Count));            
        }
    }

    [Fact]
    public async Task ForestModelWithTopAndSmallerPageSize()
    {
        string query = "/forest?$top=3";
        var querySettings = new QuerySettings { ODataSettings = new ODataSettings { HandleNullPropagation = HandleNullPropagationOption.False, PageSize = 2 } };
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
        }
    }

    [Fact]
    public async void ForestModelWithTopAndLargerPageSize()
    {
        string query = "/forest?$top=2";
        var querySettings = new QuerySettings { ODataSettings = new ODataSettings { HandleNullPropagation = HandleNullPropagationOption.False, PageSize = 4 } };
        Test(Get<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, querySettings: querySettings));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query, querySettings: querySettings));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
        }
    }

    [Fact]
    public async Task ForestModelWithTopAndSmallerPageSizeNextLink()
    {
        const int pageSize = 2;
        const string query = "/forest?$top=3";
        var querySettings = new QuerySettings { ODataSettings = new ODataSettings { HandleNullPropagation = HandleNullPropagationOption.False, PageSize = pageSize } };
        ODataQueryOptions<ForestModel> options = ODataHelpers.GetODataQueryOptions<ForestModel>
        (
            query,
            serviceProvider
        );

        Test(Get<ForestModel, Forest>(query, options, querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, options, querySettings));
        Test
        (
            await GetUsingCustomNameSpace<ForestModel, Forest>
            (
                query,
                ODataHelpers.GetODataQueryOptions<ForestModel>
                (
                    query,
                    serviceProvider,
                    "com.FooBar"
                ),
                querySettings
            )
        );

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);

            Uri nextPageLink = options.Request.ODataFeature().NextLink;
            Assert.NotNull(nextPageLink);
            Assert.Equal("localhost:16324/forest?$top=1&$skip=2", nextPageLink.AbsoluteUri);
            Assert.Contains("$top=1", nextPageLink.Query);
            Assert.Contains("$skip=2", nextPageLink.Query);
        }
    }

    [Fact]
    public async Task ForestModelWithTopAndLargerPageSizeNextLink()
    {
        const int pageSize = 4;
        const string query = "/forest?$top=3";
        var querySettings = new QuerySettings { ODataSettings = new ODataSettings { HandleNullPropagation = HandleNullPropagationOption.False, PageSize = pageSize } };
        ODataQueryOptions<ForestModel> options = ODataHelpers.GetODataQueryOptions<ForestModel>
        (
            query,
            serviceProvider
        );

        Test(Get<ForestModel, Forest>(query, options, querySettings));
        Test(await GetAsync<ForestModel, Forest>(query, options, querySettings));
        Test
        (
            await GetUsingCustomNameSpace<ForestModel, Forest>
            (
                query,
                ODataHelpers.GetODataQueryOptions<ForestModel>
                (
                    query,
                    serviceProvider,
                    "com.FooBar"
                ),
                querySettings
            )
        );

        void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Null(options.Request.ODataFeature().NextLink);
        }
    }

    [Fact]
    public async Task ForestModelOrderByPropertyOfChildReferenceOfReference()
    {        
        string query = "/forest?$expand=PrimaryDc($expand=SelectedBackup)&$orderby=PrimaryDc/SelectedBackup/DateCreated";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal("Zulauf Forest", collection.ElementAt(0).ForestName);            
            Assert.Equal("Rolfson Forest", collection.ElementAt(1).ForestName);
            Assert.Equal("Abernathy Forest", collection.ElementAt(2).ForestName);
        }
    }

    [Fact]
    public async Task ForestModelFilteringOnRoot_AndChildEntityCollection_WithMatches()
    {
        const string query = "/forest?$top=5&$expand=DomainControllers/Entry/Dc($expand=Backups($filter=Location/NetworkInformation/Address eq 'Azure blob storage'))&$filter=ForestName eq 'Abernathy Forest'";        
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Single(collection);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.Single(collection.First().DomainControllers.ElementAt(0).Entry.Dc.Backups);
            Assert.Single(collection.First().DomainControllers.ElementAt(1).Entry.Dc.Backups);
            Assert.Single(collection.First().DomainControllers.ElementAt(2).Entry.Dc.Backups);
            Assert.Empty(collection.First().DomainControllers.ElementAt(3).Entry.Dc.Backups);
        }
    }

    [Fact]
    public async Task ForestModelFilteringChildComplexCollection_WithNoMatches()
    {
        const string query = "/forest?$select=DomainControllers($filter=DcCredentials/Username eq 'Some User')";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(0, collection.ElementAt(0).DomainControllers.Count);
            Assert.Equal(0, collection.ElementAt(1).DomainControllers.Count);
            Assert.Equal(0, collection.ElementAt(2).DomainControllers.Count);            
        }
    }

    [Fact]
    public async Task ForestModelFilteringChildComplexCollection_WithMatches()
    {
        const string query = "/forest?$select=DomainControllers($filter=DcCredentials/Username eq 'administrator1')&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(1, collection.ElementAt(0).DomainControllers.Count);
            Assert.Equal(1, collection.ElementAt(1).DomainControllers.Count);
            Assert.Equal(2, collection.ElementAt(2).DomainControllers.Count);
        }
    }

    [Fact]
    public async Task ForestModelFilterEqOnNestedEntityInComplexCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FullyQualifiedDomainName eq 'dc1.abernathy.com')&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(1, collection.ElementAt(0).DomainControllers.Count);
            Assert.Equal(0, collection.ElementAt(1).DomainControllers.Count);
            Assert.Equal(0, collection.ElementAt(2).DomainControllers.Count);
        }
    }

    [Fact]
    public async Task ForestModelFilterContainsOnNestedEntityInComplexCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=contains(FullyQualifiedDomainName, 'dc1'))&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers);
            Assert.Equal("dc1.abernathy.com", collection.ElementAt(0).DomainControllers.First().Entry.Dc.FullyQualifiedDomainName);
            Assert.Single(collection.ElementAt(1).DomainControllers);
            Assert.Equal("dc1.rolfson.com", collection.ElementAt(1).DomainControllers.First().Entry.Dc.FullyQualifiedDomainName);
            Assert.Single(collection.ElementAt(2).DomainControllers);
            Assert.Equal("dc1.zulauf.net", collection.ElementAt(2).DomainControllers.First().Entry.Dc.FullyQualifiedDomainName);
        }
    }

    [Fact]
    public async Task ForestModelFilterEqOnNestedNestedEntityInComplexCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($expand=AdminGroup/UserObjects/User($filter=FirstName eq 'Edgar'))&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {            
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Equal("Edgar", collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User).ElementAt(0).FirstName);
            Assert.Empty(collection.ElementAt(1).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Empty(collection.ElementAt(2).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
        }
    }

    [Fact]
    public async Task ForestModelFilterContainsOnNestedNestedEntityInComplexCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($expand=AdminGroup/UserObjects/User($filter=contains(LastName, 'McGhee')))&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Equal("McGhee", collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User).ElementAt(0).LastName);
            Assert.Empty(collection.ElementAt(1).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Single(collection.ElementAt(2).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Equal("McGhee", collection.ElementAt(2).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User).ElementAt(0).LastName);
        }
    }

    [Fact]
    public async Task ForestModelFilterContainsOnNestedEntityInComplexCollection_AndFilterContainsOnNestedNestedEntityInComplexCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FullyQualifiedDomainName eq 'dc2.abernathy.com';$expand=AdminGroup/UserObjects/User($filter=contains(LastName, 'McGhee')))&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers);
            Assert.Equal("dc2.abernathy.com", collection.ElementAt(0).DomainControllers.ElementAt(0).Entry.Dc.FullyQualifiedDomainName);
            Assert.Single(collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Equal("McGhee", collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User).ElementAt(0).LastName);
            Assert.Empty(collection.ElementAt(1).DomainControllers);
            Assert.Empty(collection.ElementAt(2).DomainControllers);
        }
    }

    [Fact]
    public async Task ForestModelFilterContainsOnNestedEntityInComplexCollection_AndFilterContainsOnNestedNestedEntityInComplexCollection_AndFilterNestedNestedNestedEntityInEntityCollection_WithMatches()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FullyQualifiedDomainName eq 'dc2.abernathy.com';$expand=AdminGroup/UserObjects/User($filter=contains(LastName, 'McGhee')),Backups($filter=Location/NetworkInformation/Address eq 'Azure blob storage'))&orderby=ForestName asc";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers);
            Assert.Equal("dc2.abernathy.com", collection.ElementAt(0).DomainControllers.ElementAt(0).Entry.Dc.FullyQualifiedDomainName);
            Assert.Single(collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User));
            Assert.Equal("McGhee", collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.AdminGroup.UserObjects).Select(u => u.User).ElementAt(0).LastName);
            Assert.Single(collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.Backups));
            Assert.Equal("Azure blob storage", collection.ElementAt(0).DomainControllers.SelectMany(e => e.Entry.Dc.Backups).ElementAt(0).Location!.NetworkInformation!.Address);
            Assert.Empty(collection.ElementAt(1).DomainControllers);
            Assert.Empty(collection.ElementAt(2).DomainControllers);
        }
    }

    [Fact]
    public async Task ForestModelFilteringOnRoot_AndChildEntityCollection_WithNoMatches()
    {
        const string query = "/forest?$top=5&$expand=DomainControllers/Entry/Dc($expand=Backups($filter=Location/NetworkInformation/Address eq 'Azure blob storage'))&$filter=ForestName eq 'Fake Forest'";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Empty(collection);
        }
    }

    [Fact]
    public async Task ForestModel_NestedFilterAndSelect_ShouldReturnEntityWithSelectedPropertiesAndFilteredLiteralCollection()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($expand=Backups($select=Values($filter=$this gt 10), Location))";
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.All(collection
                .SelectMany(m => m.DomainControllers)
                .SelectMany(entry => entry.Entry.Dc.Backups), backup => Assert.NotNull(backup.Location));

            Assert.All(collection
                .SelectMany(m => m.DomainControllers)
                .SelectMany(entry => entry.Entry.Dc.Backups), backup => Assert.Single(backup.Values));
        }
    }

    [Fact]
    public async Task ForestModel_SelectNestedFilterOnEnumCollectionEqOperator()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($select=FsmoRoles($filter=$this eq 'RidMaster'))&$filter=ForestName eq 'Zulauf Forest'";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Single(collection);
            Assert.Equal(2, collection.ElementAt(0).DomainControllers.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers.ElementAt(0).Entry.Dc.FsmoRoles);
            Assert.Empty(collection.ElementAt(0).DomainControllers.ElementAt(1).Entry.Dc.FsmoRoles);
            Assert.Equal(FsmoRole.RidMaster, collection.ElementAt(0).DomainControllers.ElementAt(0).Entry.Dc.FsmoRoles.Single());            
        }
    }

    [Fact]
    public async Task ForestModel_SelectNestedFilterOnEnumCollectionInOperator()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($select=FsmoRoles($filter=$this in ('RidMaster', 'DomainNamingMaster')))&$filter=ForestName eq 'Zulauf Forest'";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Single(collection);
            Assert.Equal(2, collection.First().DomainControllers.Count);
            Assert.Single(collection.First().DomainControllers.First().Entry.Dc.FsmoRoles);
            Assert.Single(collection.First().DomainControllers.Last().Entry.Dc.FsmoRoles);
            Assert.Contains(FsmoRole.RidMaster, collection.First().DomainControllers.First().Entry.Dc.FsmoRoles);
            Assert.Contains(FsmoRole.DomainNamingMaster, collection.First().DomainControllers.Last().Entry.Dc.FsmoRoles);
        }
    }

    [Fact]
    public async Task ForestModel_FilterAnyOnNestedEntityEnumCollection_ShouldReturnMatchingDCs()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FsmoRoles/any(role: role eq 'PdcEmulator'))";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers);
            Assert.Contains(FsmoRole.PdcEmulator, collection.ElementAt(0).DomainControllers.Single().Entry.Dc.FsmoRoles);
            Assert.Single(collection.ElementAt(1).DomainControllers);
            Assert.Contains(FsmoRole.PdcEmulator, collection.ElementAt(1).DomainControllers.Single().Entry.Dc.FsmoRoles);
            Assert.Single(collection.ElementAt(2).DomainControllers);
            Assert.Contains(FsmoRole.PdcEmulator, collection.ElementAt(2).DomainControllers.Single().Entry.Dc.FsmoRoles);
        }
    }

    [Fact]
    public async Task ForestModel_FilterInOnNestedEntityEnumCollection_ShouldReturnMatchingDCs()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FsmoRoles/any(role: role in ('PdcEmulator', 'SchemaMaster')))";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Equal(2, collection.ElementAt(0).DomainControllers.Count);
            Assert.Contains(collection.ElementAt(0).DomainControllers.First().Entry.Dc.FsmoRoles, ContainsRole);
            Assert.Contains(collection.ElementAt(0).DomainControllers.Last().Entry.Dc.FsmoRoles, ContainsRole);

            Assert.Equal(2, collection.ElementAt(1).DomainControllers.Count);
            Assert.Contains(collection.ElementAt(1).DomainControllers.First().Entry.Dc.FsmoRoles, ContainsRole);
            Assert.Contains(collection.ElementAt(1).DomainControllers.Last().Entry.Dc.FsmoRoles, ContainsRole);

            Assert.Equal(2, collection.ElementAt(2).DomainControllers.Count);
            Assert.Contains(collection.ElementAt(2).DomainControllers.First().Entry.Dc.FsmoRoles, ContainsRole);
            Assert.Contains(collection.ElementAt(2).DomainControllers.Last().Entry.Dc.FsmoRoles, ContainsRole);
        }

        static bool ContainsRole(FsmoRole role) =>
            role == FsmoRole.PdcEmulator || role == FsmoRole.SchemaMaster;
    }

    [Fact]
    public async Task ForestModel_UsingInFilter_OnRootEnumProperty()
    {
        const string query = "/forest?$filter=Status in ('NotHealthy', 'Recovering')&$orderby=ForestName";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(2, collection.Count);
            Assert.Equal(ForestStatusModel.Recovering, collection.First().Status);
            Assert.Equal(ForestStatusModel.NotHealthy, collection.Last().Status);            
        }
    }

    [Fact]
    public async Task ForestModel_FilterEqOnNestedEntityEnumPropertyStoredAsStringInDb_ShouldReturnMatchingDCs()
    {                
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=cast(Status, Edm.String) eq 'NotHealthy')&orderby=ForestName desc";        

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Equal(3, collection.Count);
            Assert.Single(collection.ElementAt(0).DomainControllers);
            Assert.Equal(DcStatusModel.NotHealthy, collection.ElementAt(0).DomainControllers.Single().Entry.Dc.Status);
            Assert.Empty(collection.ElementAt(1).DomainControllers);            
            Assert.Empty(collection.ElementAt(2).DomainControllers);            
        }
    }

    [Fact]
    public async Task ForestModel_FilterAnyOnPrimitiveCollection_ShouldReturnSingleEntity()
    {
        const string query = "/forest?$filter=Values/any(value: value eq 6389)";

        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Single(collection);
            Assert.Equal("Zulauf Forest", collection.First().ForestName);
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
                Assert.Equal(expected, model.Values);
            }
        }
    }

    [Fact]
    public async Task SkipBeyondAllElementsOnRootNoOrderBy()
    {
        const string query = "/forest?$skip=3";
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));
        Test(Get<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Empty(collection);
        }
    }

    [Fact]
    public async Task CancellationThrowsException()
    {
        var cancelledToken = new CancellationTokenSource(TimeSpan.Zero).Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            GetAsync<ForestModel, Forest>("/forest?$count=true", querySettings: new QuerySettings 
            { 
                AsyncSettings = new AsyncSettings { CancellationToken = cancelledToken } 
            }));
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
