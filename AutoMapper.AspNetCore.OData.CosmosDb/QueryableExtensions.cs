using AutoMapper.AspNet.OData;
using AutoMapper.Extensions.ExpressionMapping;
using LogicBuilder.Expressions.Utils.Expansions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoMapper.AspNetCore.OData.CosmosDb;

public static class QueryableExtensions
{
    public static async Task<IQueryable<TModel>> GetQueryAsync<TModel, TData>(
        this IQueryable<TData> query, 
        IMapper mapper, 
        ODataQueryOptions<TModel> options, 
        QuerySettings querySettings = null) where TModel : class
    {
        Expression<Func<TModel, bool>> filter = options.ToFilterExpression(
            querySettings?.ODataSettings?.HandleNullPropagation ?? HandleNullPropagationOption.False,
            querySettings?.ODataSettings?.TimeZone);

        await query.ApplyOptionsAsync(mapper, filter, options, querySettings).ConfigureAwait(false);
        return query.GetQueryable(mapper, options, querySettings, filter);
    }

    public static async Task ApplyOptionsAsync<TModel, TData>(
        this IQueryable<TData> query, 
        IMapper mapper, Expression<Func<TModel, bool>> filter, 
        ODataQueryOptions<TModel> options, 
        QuerySettings querySettings)
    {
        ApplyOptions(options, querySettings);

        if (options.Count?.Value == true)
        {
            var cancellationToken = querySettings?.AsyncSettings?.CancellationToken ?? CancellationToken.None;
        
            options.AddCountOptionsResult(
                await query.QueryCountAsync(mapper, filter, cancellationToken).ConfigureAwait(false));
        }
    }

    private static void ApplyOptions<TModel>(ODataQueryOptions<TModel> options, QuerySettings querySettings)
    {
        options.AddExpandOptionsResult();
        if (querySettings?.ODataSettings?.PageSize.HasValue == true)
            options.AddNextLinkOptionsResult(querySettings.ODataSettings.PageSize.Value);
    }

    private static IQueryable<TModel> GetQueryable<TModel, TData>(this IQueryable<TData> query,
            IMapper mapper,
            ODataQueryOptions<TModel> options,
            QuerySettings querySettings,
            Expression<Func<TModel, bool>> filter)
            where TModel : class
    {

        var expansions = options.SelectExpand.GetExpansions(typeof(TModel));

        return query.GetQuery
        (
            mapper,
            filter,
            options.GetQueryableExpression(querySettings?.ODataSettings),
            expansions
                .Select(list => new List<Expansion>(list))
                .BuildIncludes<TModel>(options.SelectExpand.GetSelects())
                .ToList(),
            querySettings?.ProjectionSettings
        ).UpdateQueryableExpression(expansions, options.Context);
    }

    private static IQueryable<TModel> GetQuery<TModel, TData>(this IQueryable<TData> query,
            IMapper mapper,
            Expression<Func<TModel, bool>> filter = null,
            Expression<Func<IQueryable<TModel>, IQueryable<TModel>>> queryFunc = null,
            IEnumerable<Expression<Func<TModel, object>>> includeProperties = null,
            ProjectionSettings projectionSettings = null)
    {
        Expression<Func<TData, bool>> f = mapper.MapExpression<Expression<Func<TData, bool>>>(filter);
        Func<IQueryable<TData>, IQueryable<TData>> mappedQueryFunc = mapper.MapExpression<Expression<Func<IQueryable<TData>, IQueryable<TData>>>>(queryFunc)?.Compile();

        if (filter is not null)
            query = query.Where(f);

        return mappedQueryFunc is not null
                ? mapper.ProjectTo(mappedQueryFunc(query), projectionSettings?.Parameters, GetIncludes())
                : mapper.ProjectTo(query, projectionSettings?.Parameters, GetIncludes());

        Expression<Func<TModel, object>>[] GetIncludes() => 
            includeProperties?.ToArray() ?? new Expression<Func<TModel, object>>[] { };
    }

    private static async Task<int> QueryCountAsync<TModel, TData>(
        this IQueryable<TData> query, 
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
}
