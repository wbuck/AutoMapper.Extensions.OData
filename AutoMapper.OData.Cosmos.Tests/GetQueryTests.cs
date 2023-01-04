using AutoMapper.AspNet.OData;
using AutoMapper.OData.Cosmos.Tests.Entities;
using AutoMapper.OData.Cosmos.Tests.Infrastructure;
using AutoMapper.OData.Cosmos.Tests.Mappings;
using AutoMapper.OData.Cosmos.Tests.Models;
using AutoMapper.OData.Cosmos.Tests.Persistence;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using System.Diagnostics;

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
        string query = "/forest?$top=5&$expand=DomainControllers/Dc&$filter=ForestName eq 'Rolfson Forest'&$orderby=ForestName desc";
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
        const string query = "/forest?$top=5&$expand=DomainControllers/Dc&$filter=ForestName ne 'Zulauf Forest'&$orderby=ForestName desc";
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
        string query = "/forest?$top=2&$expand=DomainControllers/Dc&$orderby=ForestName desc";
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
        string query = "/forest?$top=5&$select=ForestName&$expand=DomainControllers/Dc($select=FullyQualifiedDomainName,Backups;$expand=Backups($select=ForestId, Location))&$filter=ForestName ne 'Zulauf Forest'&$orderby=ForestName desc";
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
        const string query = "/forest?$top=5&$expand=DomainControllers/Dc($expand=Backups)&$filter=ForestName ne 'Abernathy Forest'&$orderby=ForestName desc";
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
        const string query = "/forest?$expand=DomainControllers/Dc&$filter=DomainControllers/any(entry: entry/Dc/FullyQualifiedDomainName eq 'dc1.abernathy.com')&$orderby=ForestName desc";
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

    [Fact]
    public async Task ForestModelExpandDcExpandBackupOrderByForestNameSkipTakeWithCount()
    {
        string query = "/forest?$skip=2&$top=1&$expand=DomainControllers/Dc($expand=Backups)&$orderby=ForestName desc&$count=true";
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
        const string query = "/forest?$top=5&$expand=DomainControllers/Dc($expand=Backups($filter=Location/NetworkInformation/Address eq 'Azure blob storage'))&$filter=ForestName eq 'Abernathy Forest'";        
        Test(Get<ForestModel, Forest>(query));
        Test(await GetAsync<ForestModel, Forest>(query));
        Test(await GetUsingCustomNameSpace<ForestModel, Forest>(query));

        static void Test(ICollection<ForestModel> collection)
        {
            Assert.Single(collection);
            Assert.Equal(4, collection.First().DomainControllers.Count);
            Assert.Equal(1, collection.First().DomainControllers.ElementAt(0).Entry.Dc.Backups.Count);
            Assert.Equal(1, collection.First().DomainControllers.ElementAt(1).Entry.Dc.Backups.Count);
            Assert.Equal(1, collection.First().DomainControllers.ElementAt(2).Entry.Dc.Backups.Count);
            Assert.Equal(0, collection.First().DomainControllers.ElementAt(3).Entry.Dc.Backups.Count);
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
        const string query = "/forest?$select=DomainControllers($filter=DcCredentials/Username eq 'administrator1')";
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
    public async Task ForestModelFilteringChildComplexCollection_WithMatches2()
    {
        const string query = "/forest?$expand=DomainControllers/Entry/Dc($filter=FullyQualifiedDomainName eq 'dc1.abernathy.com')";
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
    public async Task ForestModelFilteringOnRoot_AndChildEntityCollection_WithNoMatches()
    {
        const string query = "/forest?$top=5&$expand=DomainControllers/Dc($expand=Backups($filter=Location/NetworkInformation/Address eq 'Azure blob storage'))&$filter=ForestName eq 'Fake Forest'";
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
        const string query = "/forest?$expand=DomainControllers/Dc($expand=Backups($select=Values($filter=$this gt 10), Location))";
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
