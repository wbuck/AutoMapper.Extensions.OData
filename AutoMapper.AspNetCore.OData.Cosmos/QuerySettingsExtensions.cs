using System.Threading;

namespace AutoMapper.AspNet.OData;

internal static class QuerySettingsExtensions
{
    public static CancellationToken GetCancellationToken(this QuerySettings querySettings) =>
        querySettings?.AsyncSettings?.CancellationToken ?? CancellationToken.None;
}
