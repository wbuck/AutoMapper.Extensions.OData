using AutoMapper.AspNet.OData;
using System.Threading;

namespace AutoMapper.AspNetCore.OData.CosmosDb;
internal static class QuerySettingsExtensions
{
    public static CancellationToken GetCancellationToken(this QuerySettings querySettings) =>
        querySettings?.AsyncSettings?.CancellationToken ?? CancellationToken.None;
}
