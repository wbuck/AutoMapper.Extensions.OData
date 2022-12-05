using AutoMapper.OData.Cosmos.Tests.Data;
using AutoMapper.OData.Cosmos.Tests.Entities;
using Microsoft.Azure.Cosmos;

namespace AutoMapper.OData.Cosmos.Tests.Persistence;

internal static class ContainerExtensions
{
    public static Task SeedAsync(this Container dbContainer, CancellationToken cancellationToken = default)
    {
        var data = DatabaseSeeder.GenerateData();
        var tasks = new List<Task>(data.Count);

        foreach (var forest in data)
        {
            var key = new PartitionKey(forest.ForestId.ToString());
            tasks.Add(dbContainer.CreateItemAsync(forest, key, cancellationToken: cancellationToken));            
        }

        return Task.WhenAll(tasks);
    }
}
