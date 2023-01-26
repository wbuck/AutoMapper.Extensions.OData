using AutoMapper.AspNet.OData.Visitors;
using AutoMapper.Extensions.ExpressionMapping;
using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AutoMapper.AspNet.OData;

public static class QueryableExtensions
{
    public static ICollection<TModel> Get<TModel, TData>(this IQueryable<TData> query,
        IMapper mapper, ODataQueryOptions<TModel> options, QuerySettings querySettings = null)
         where TModel : class
    {
        IQueryable<TModel> modelQuery = GetQuery(query, mapper, options, querySettings);
        return modelQuery.ToArray();
    }

    public static async Task<ICollection<TModel>> GetAsync<TModel, TData>(this IQueryable<TData> query, 
        IMapper mapper, ODataQueryOptions<TModel> options, QuerySettings querySettings = null)
            where TModel : class
    {
        IQueryable<TModel> modelQuery = 
            await GetQueryAsync(query, mapper, options, querySettings).ConfigureAwait(false);

        return await modelQuery.ExecuteQueryAsync(querySettings.GetCancellationToken())
            .ConfigureAwait(false);
    }

    public static async Task<IQueryable<TModel>> GetQueryAsync<TModel, TData>(
        this IQueryable<TData> query, 
        IMapper mapper, 
        ODataQueryOptions<TModel> options, 
        QuerySettings querySettings = null) where TModel : class
    {
        query = query ?? throw new ArgumentNullException(nameof(query));
        mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        options = options ?? throw new ArgumentNullException(nameof(options));

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
        query = query ?? throw new ArgumentNullException(nameof(query));
        mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        options = options ?? throw new ArgumentNullException(nameof(options));

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
            .BuildSelectExpressions<TModel>(selects)
            .ToList();

        return query.GetQuery
        (
            mapper,
            filter,
            options.GetQueryableExpression(querySettings?.ODataSettings),
            includes,
            querySettings?.ProjectionSettings
        ).ApplyFilters(expansions.Concat(selects).ToList().FilterSelects(), options.Context);
    }

    private static List<List<PathSegment>> FilterSelects(this List<List<PathSegment>> pathSegments)
    {
        List<List<PathSegment>> filtered = new(pathSegments.Count);
        foreach (List<PathSegment> segments in pathSegments)
        {
            PathSegment lastSegment = segments.Last();
            if (lastSegment.FilterOptions is not null || lastSegment.QueryOptions is not null)
            {
                filtered.Add(segments);
            }

            var selectSegments = lastSegment.SelectPaths;
            if (selectSegments is not null)
            {
                filtered.AddRange
                (
                    selectSegments
                        .Where(s => s.Last().FilterOptions is not null || s.Last().QueryOptions is not null)
                        .Select(s => segments.Concat(s).ToList())
                );
            }
        }
        return filtered;
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
            includeProperties?.ToArray() ?? Array.Empty<Expression<Func<TModel, object>>>();
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

    private static IQueryable<TModel> ApplyFilters<TModel>(
            this IQueryable<TModel> query, List<List<PathSegment>> selects, ODataQueryContext context)
    {
        List<List<PathSegment>> memberFilters = GetMemberFilters();
        List<List<PathSegment>> collectionFilters = GetMemberCollectionFilters();

        List<List<PathSegment>> methods = GetQueryMethods();

        if (!memberFilters.Any() && !collectionFilters.Any() && !methods.Any())
            return query;

        Expression expression = query.Expression;

        methods.ForEach
        (
            segments => expression = QueryMethodAppender.AppendFilters(expression, segments, context)
        );

        memberFilters.ForEach
        (
            segments => expression = MemberFilterAppender.AppendFilters(expression, segments, context)
        );

        collectionFilters.ForEach
        (
            segments => expression = MemberCollectionFilterAppender.AppendFilters(expression, segments, context)
        );

        return query.Provider.CreateQuery<TModel>(expression);

        List<List<PathSegment>> GetMemberFilters() =>
            selects.Where(s => 
            {
                ref PathSegment lastSegment = ref GetLastSegment(s);
                return !lastSegment.MemberType.IsList() && lastSegment.FilterOptions is not null;
            }).ToList();

        List<List<PathSegment>> GetMemberCollectionFilters() =>
            selects.Where(s => 
            {
                ref PathSegment lastSegment = ref GetLastSegment(s);
                return lastSegment.MemberType.IsList() && lastSegment.FilterOptions is not null;
            }).ToList();

        List<List<PathSegment>> GetQueryMethods() =>
            selects.Where(s => s.Last().QueryOptions is not null).ToList();

        ref PathSegment GetLastSegment(List<PathSegment> pathSegments)
        {
            Span<PathSegment> segments = CollectionsMarshal.AsSpan(pathSegments);
            return ref segments[^1];
        }
    }

    private static CancellationToken GetCancellationToken(this QuerySettings querySettings) =>
        querySettings?.AsyncSettings?.CancellationToken ?? CancellationToken.None;
}
