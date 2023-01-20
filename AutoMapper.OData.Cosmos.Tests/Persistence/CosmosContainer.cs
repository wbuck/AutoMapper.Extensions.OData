using AutoMapper.OData.Cosmos.Tests.Data;
using AutoMapper.OData.Cosmos.Tests.Entities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace AutoMapper.OData.Cosmos.Tests.Persistence;

[CollectionDefinition(nameof(CosmosContainer))]
public sealed class CosmosContainerFixture : ICollectionFixture<CosmosContainer>
{ }

public sealed class CosmosContainer : IAsyncLifetime
{
    private const string DatabaseId = "ForestDatabase";
    private const string ContainerId = "ForestContainer";
    private const string PartitionKey = "/forestId";

    private readonly CosmosDbTestcontainer testContainer;
    private readonly CosmosDbTestcontainerConfiguration configuration;

    private CosmosClient? client;

    public CosmosContainer()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
            //Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        this.configuration = new();
        this.testContainer = new TestcontainersBuilder<CosmosDbTestcontainer>()
            .WithDatabase(this.configuration)
            .WithCleanUp(true)
            .Build();
    }

    //localhost:49162/_explorer/index.html
    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await this.testContainer.StartAsync(cts.Token).ConfigureAwait(false);

        this.client = await CreateCosmosClientAsync(cts.Token).ConfigureAwait(false);
    }

    public Container GetContainer() =>
        this.client?.GetContainer(DatabaseId, ContainerId) 
            ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} must be called");

    public async Task DisposeAsync()
    {
        await this.testContainer.DisposeAsync().ConfigureAwait(false);
        this.configuration.Dispose();
    }


    private async Task<CosmosClient> CreateCosmosClientAsync(CancellationToken cancellationToken = default)
    {
        var client = new CosmosClient(testContainer.ConnectionString, new() 
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => this.testContainer.HttpClient,
            AllowBulkExecution = true,           
            SerializerOptions = new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
        });

        try
        {
            var databaseResponse = await client
                .CreateDatabaseIfNotExistsAsync(DatabaseId, 4000, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var containerResponse = await databaseResponse.Database.DefineContainer(ContainerId, PartitionKey)
                .WithIndexingPolicy()
                    .WithIncludedPaths()
                        .Path("/*")
                    .Attach()
                    .WithCompositeIndex()
                        .Path("/name")
                        .Path("/createdDate")
                    .Attach()
                .Attach()
                .CreateIfNotExistsAsync(4000, cancellationToken).ConfigureAwait(false);
                                
            await containerResponse.Container.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }
        
        return client;
    }    
}
