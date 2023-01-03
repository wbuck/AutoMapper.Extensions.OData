using AutoMapper.AspNet.OData.Visitors;
using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
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
        ).ApplyFilters(expansions.Concat(selects).ToList().ToExpansionOptions(), options.Context);
    }

    private static List<ODataExpansionOptions> ToExpansionOptions(this IEnumerable<PathSegment> pathSegments) =>
        pathSegments.Select(s => new ODataExpansionOptions
        {
            MemberName = s.MemberName,
            MemberType = s.MemberType,
            ParentType = s.ParentType,
            FilterOptions = s.FilterOptions,
            QueryOptions = s.QueryOptions
        }).ToList();

    private static List<List<ODataExpansionOptions>> ToExpansionOptions(this List<List<PathSegment>> pathSegments)
    {
        List<List<ODataExpansionOptions>> filtered = new(pathSegments.Count);
        foreach (List<PathSegment> segments in pathSegments)
        {
            PathSegment lastSegment = segments.Last();
            if (lastSegment.FilterOptions is not null || lastSegment.QueryOptions is not null)
            {
                filtered.Add(segments.ToExpansionOptions());
            }

            var selectSegments = lastSegment.SelectPaths;
            if (selectSegments is not null)
            {
                filtered.AddRange
                (
                    selectSegments
                        .Where(s => s.Last().FilterOptions is not null || s.Last().QueryOptions is not null)
                        .Select(s => segments.Concat(s).ToExpansionOptions())
                );
            }
        }

        return filtered;
    }

    private static List<List<ODataExpansionOptions>> ToExpansionOptions2(this List<List<PathSegment>> pathSegments)
    {
        List<List<ODataExpansionOptions>> filtered = new(pathSegments.Count);
        foreach (List<PathSegment> segments in pathSegments)
        {
            if (segments.Any(s => s.FilterOptions is not null || s.QueryOptions is not null))
            {
                int index = segments.FindIndex(s => s.FilterOptions is not null);
                filtered.Add(segments.Take(index + 1).ToExpansionOptions());
            }

            foreach (var path in segments)
            {
                if (path.SelectPaths is null)
                    continue;

                foreach (var selectPath in path.SelectPaths
                        .Where(p => p.Any(s => s.FilterOptions is not null || s.QueryOptions is not null))
                        .Select(p => segments.Concat(p).ToList()))
                {
                    int index = selectPath.FindIndex(s => s.FilterOptions is not null);
                    if (index > -1)
                    {
                        filtered.Add(selectPath.Take(index + 1).ToExpansionOptions());
                    }
                }

                //filtered.AddRange
                //(
                //    path.SelectPaths
                //        .Where(p => p.Any(s => s.FilterOptions is not null || s.QueryOptions is not null))
                //        .Select(p => segments.Concat(p).ToExpansionOptions())
                //);
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

    private static IQueryable<TModel> ApplyFilters<TModel>(
            this IQueryable<TModel> query, List<List<ODataExpansionOptions>> expansions, ODataQueryContext context)
    {
        List<List<ODataExpansionOptions>> filters = GetFilters();
        List<List<ODataExpansionOptions>> methods = GetQueryMethods();

        if (!filters.Any() && !methods.Any())
            return query;

        Expression expression = query.Expression;

        if (methods.Any())
            expression = UpdateProjectionMethodExpression(expression);

        if (filters.Any())//do filter last so it runs before a Skip or Take call.
            expression = UpdateProjectionFilterExpression(expression);

        return query.Provider.CreateQuery<TModel>(expression);

        Expression UpdateProjectionFilterExpression(Expression projectionExpression)
        {
            filters.ForEach
            (
                filterList => projectionExpression = FilterMethodAppender.AppendFilters
                (
                    projectionExpression,
                    filterList,
                    context
                )
            );

            return projectionExpression;
        }

        Expression UpdateProjectionMethodExpression(Expression projectionExpression)
        {
            methods.ForEach
            (
                methodList => projectionExpression = QueryMethodAppender.AppendQuery
                (
                    projectionExpression,
                    methodList,
                    context
                )
            );

            return projectionExpression;
        }

        List<List<ODataExpansionOptions>> GetFilters()
            => expansions.Aggregate(new List<List<ODataExpansionOptions>>(), (listOfLists, nextList) =>
            {
                var filterNextList = nextList.Aggregate(new List<ODataExpansionOptions>(), (list, next) =>
                {
                    if (next.FilterOptions != null)
                    {
                        list = list.ConvertAll
                        (
                            exp => new ODataExpansionOptions
                            {
                                MemberName = exp.MemberName,
                                MemberType = exp.MemberType,
                                ParentType = exp.ParentType,
                            }
                        );//new list removing filter

                        list.Add
                        (
                            new ODataExpansionOptions
                            {
                                MemberName = next.MemberName,
                                MemberType = next.MemberType,
                                ParentType = next.ParentType,
                                FilterOptions = new FilterOptions(next.FilterOptions.FilterClause)
                            }
                        );//add expansion with filter

                        listOfLists.Add(list.ToList()); //Add the whole list to the list of filter lists
                                                        //Only the last item in each list has a filter
                                                        //Filters for parent expansions exist in their own lists
                        return list;
                    }

                    list.Add(next);

                    return list;
                });

                return listOfLists;
            });

        List<List<ODataExpansionOptions>> GetQueryMethods()
            => expansions.Aggregate(new List<List<ODataExpansionOptions>>(), (listOfLists, nextList) =>
            {
                var filterNextList = nextList.Aggregate(new List<ODataExpansionOptions>(), (list, next) =>
                {
                    if (next.QueryOptions != null)
                    {
                        list = list.ConvertAll
                        (
                            exp => new ODataExpansionOptions
                            {
                                MemberName = exp.MemberName,
                                MemberType = exp.MemberType,
                                ParentType = exp.ParentType,
                            }
                        );//new list removing query options

                        list.Add
                        (
                            new ODataExpansionOptions
                            {
                                MemberName = next.MemberName,
                                MemberType = next.MemberType,
                                ParentType = next.ParentType,
                                QueryOptions = new QueryOptions(next.QueryOptions.OrderByClause, next.QueryOptions.Skip, next.QueryOptions.Top)
                            }
                        );//add expansion with query options

                        listOfLists.Add(list.ToList()); //Add the whole list to the list of query method lists
                                                        //Only the last item in each list has a query method
                                                        //Query methods for parent expansions exist in their own lists
                        return list;
                    }

                    list.Add(next);

                    return list;
                });

                return listOfLists;
            });
    }
}
