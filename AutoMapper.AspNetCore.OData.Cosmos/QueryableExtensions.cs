using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoMapper.AspNet.OData;

public static class QueryableExtensions
{
    public static ICollection<TModel> Get<TModel, TData>(this IQueryable<TData> query,
        IMapper mapper, ODataQueryOptions<TModel> options, QuerySettings querySettings = null)
         where TModel : class
    {
        IQueryable<TModel> modelQuery = query.GetQuery(mapper, options, querySettings);
        return modelQuery.ToArray();
    }

    public static async Task<ICollection<TModel>> GetAsync<TModel, TData>(this IQueryable<TData> query, 
        IMapper mapper, ODataQueryOptions<TModel> options, QuerySettings querySettings = null)
            where TModel : class
    {
        IQueryable<TModel> modelQuery = await query
            .GetQueryAsync(mapper, options, querySettings).ConfigureAwait(false);

        return await modelQuery.ExecuteQueryAsync(querySettings.GetCancellationToken())
            .ConfigureAwait(false);
    }

    public static async Task<IQueryable<TModel>> GetQueryAsync<TModel, TData>(
        this IQueryable<TData> query, 
        IMapper mapper, 
        ODataQueryOptions<TModel> options, 
        QuerySettings querySettings = null) where TModel : class
    {
        Expression<Func<TModel, bool>> filter = options.ToFilterExpression(
            querySettings?.ODataSettings?.HandleNullPropagation ?? HandleNullPropagationOption.False,
            querySettings?.ODataSettings?.TimeZone);

        ApplyOptions(options, querySettings);

        await query.ApplyCountQueryAsync(mapper, filter, options, querySettings)
            .ConfigureAwait(false);

        return query.GetQueryable(mapper, options, querySettings, filter);
    }

    public static IQueryable<TModel> GetQuery<TModel, TData>(
        this IQueryable<TData> query,
        IMapper mapper,
        ODataQueryOptions<TModel> options,
        QuerySettings querySettings = null) where TModel : class
    {
        Expression<Func<TModel, bool>> filter = options.ToFilterExpression(
            querySettings?.ODataSettings?.HandleNullPropagation ?? HandleNullPropagationOption.False,
            querySettings?.ODataSettings?.TimeZone);

        ApplyOptions(options, querySettings);

        query.ApplyCountQuery(mapper, filter, options);
        return query.GetQueryable(mapper, options, querySettings, filter);
    }

    private static IQueryable<TModel> GetQueryable<TModel, TData>(this IQueryable<TData> query,
            IMapper mapper,
            ODataQueryOptions<TModel> options,
            QuerySettings querySettings,
            Expression<Func<TModel, bool>> filter)
            where TModel : class
    {
        var selects = options.GetSelects();
        var expansions = options.GetExpansions();

        var includes = expansions
            .BuildIncludes<TModel>(selects)
            .ToList();

        return query.GetQuery
        (
            mapper,
            filter,
            options.GetQueryableExpression(querySettings?.ODataSettings),
            includes,
            querySettings?.ProjectionSettings
        ).UpdateQueryableExpression(expansions.ToExpansionOptions(), options.Context);
    }

    private static List<List<ODataExpansionOptions>> ToExpansionOptions(this List<List<PathSegment>> pathSegments)
    {
        List<List<ODataExpansionOptions>> options = new(pathSegments.Count);
        foreach (List<PathSegment> segments in pathSegments)
        {
            options.Add(segments.Select(s => new ODataExpansionOptions
            {
                MemberName = s.MemberName,
                MemberType = s.MemberType,
                ParentType = s.ParentType,
                FilterOptions = s.FilterOptions,
                QueryOptions = s.QueryOptions
            }).ToList());
        }
        return options;
    }

    private static IQueryable<TModel> GetQuery<TModel, TData>(this IQueryable<TData> query,
            IMapper mapper,
            Expression<Func<TModel, bool>> filter = null,
            Expression<Func<IQueryable<TModel>, IQueryable<TModel>>> queryFunc = null,
            IEnumerable<Expression<Func<TModel, object>>> includeProperties = null,
            ProjectionSettings projectionSettings = null)
    {
        Expression<Func<TData, bool>> f = mapper.MapExpression<Expression<Func<TData, bool>>>(filter);

        Func<IQueryable<TData>, IQueryable<TData>> mappedQueryFunc = 
            mapper.MapExpression<Expression<Func<IQueryable<TData>, IQueryable<TData>>>>(queryFunc)?.Compile();

        if (filter is not null)
            query = query.Where(f);

        return mappedQueryFunc is not null
                ? mapper.ProjectTo(mappedQueryFunc(query), projectionSettings?.Parameters, GetIncludes())
                : mapper.ProjectTo(query, projectionSettings?.Parameters, GetIncludes());

        Expression<Func<TModel, object>>[] GetIncludes() => 
            includeProperties?.ToArray() ?? new Expression<Func<TModel, object>>[] { };
    }

    private static void ApplyCountQuery<TModel, TData>(this IQueryable<TData> query,
        IMapper mapper, Expression<Func<TModel, bool>> filter, ODataQueryOptions<TModel> options)
    {
        if (options.Count?.Value == true)
        {
            options.AddCountOptionsResult
            (
                query.QueryCount(mapper, filter)
            );
        }
    }

    private static int QueryCount<TModel, TData>(this IQueryable<TData> query, 
        IMapper mapper, Expression<Func<TModel, bool>> filter)
    {
        if (filter is not null)
        {
            query = query.Where
            (
                mapper.MapExpression<Expression<Func<TData, bool>>>(filter)
            );
        }
        return query.Count();
    }

    private static async Task ApplyCountQueryAsync<TModel, TData>(this IQueryable<TData> query,
        IMapper mapper, 
        Expression<Func<TModel, bool>> filter,
        ODataQueryOptions<TModel> options,
        QuerySettings querySettings)
    {
        if (options.Count?.Value == true)
        {
            options.AddCountOptionsResult
            (
                await query.QueryCountAsync(mapper, filter, querySettings.GetCancellationToken())
                    .ConfigureAwait(false)
            );
        }
    }

    private static async Task<int> QueryCountAsync<TModel, TData>(this IQueryable<TData> query, 
        IMapper mapper, 
        Expression<Func<TModel, bool>> filter, 
        CancellationToken cancellationToken = default)
    {
        if (filter is not null)
        {
            query = query.Where
            (
                mapper.MapExpression<Expression<Func<TData, bool>>>(filter)
            );
        }                    
        return (await query.CountAsync(cancellationToken).ConfigureAwait(false)).Resource;
    }

    private static async Task<ICollection<TModel>> ExecuteQueryAsync<TModel>(
        this IQueryable<TModel> query, CancellationToken cancellationToken = default)
    {
        using var iterator = query.ToFeedIterator();
        return 
        (
            await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false)
        ).Resource.ToArray();
    }

    private static void ApplyOptions<TModel>(ODataQueryOptions<TModel> options, QuerySettings querySettings)
    {
        options.AddExpandOptionsResult();
        if (querySettings?.ODataSettings?.PageSize.HasValue == true)
            options.AddNextLinkOptionsResult(querySettings.ODataSettings.PageSize.Value);
    }
}
