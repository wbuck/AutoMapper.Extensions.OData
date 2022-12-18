#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal class SelectPathSegment
{
    private readonly SelectItem selectItem;    
    private readonly IReadOnlyCollection<string>? selects;

    public SelectPathSegment(SelectItem selectItem, ODataPathSegment pathSegment)
        : this(selectItem, pathSegment, null)
    { }

    public SelectPathSegment(SelectItem selectItem, ODataPathSegment pathSegment, IReadOnlyCollection<string>? selects)
    {
        this.selectItem = selectItem;
        this.selects = selects;
        PathSegment = pathSegment;        
    }

    public ODataPathSegment PathSegment { get; }

    public string Identifier => 
        PathSegment.Identifier;

    public bool IsNavigationPropertySegment() =>
        PathSegment is NavigationPropertySegment;

    public FilterOptions? GetFilter(Type memberType)
    {
        if (TryGetExpandedNavigationSelectItem(out var item)
            && memberType.IsList() && item.FilterOption is not null)
        {
            return new FilterOptions(item.FilterOption);
        }
        return null;
    }

    public QueryOptions? GetQuery()
    {
        if (TryGetExpandedNavigationSelectItem(out var item)
            && (item.OrderByOption is not null || item.SkipOption.HasValue || item.TopOption.HasValue))
        {
            return new QueryOptions(item.OrderByOption!, (int?)item.SkipOption, (int?)item.TopOption);
        }
        return null;
    }

    public bool HasSelects()
    {        
        if (TryGetExpandedNavigationSelectItem(out var item) && item.SelectAndExpand is not null)
        {
            return item.SelectAndExpand.SelectedItems
                .OfType<PathSelectItem>()
                .Any();
        }
        return this.selects is not null && this.selects.Any();
    }

    public List<string> GetSelects()
    {
        if (TryGetExpandedNavigationSelectItem(out var item))
            return GetSelects(item.SelectAndExpand);

        if (this.selects is not null && this.selects.Any())
            return this.selects.ToList();

        return new();
    }

    public bool TryGetExpandedNavigationSelectItem([MaybeNullWhen(false)] out ExpandedNavigationSelectItem item)
    {
        if (PathSegment is NavigationPropertySegment 
            && this.selectItem is ExpandedNavigationSelectItem navItem)
        {
            item = navItem;
            return true;
        }
        item = null;
        return false;
    }

    private static List<string> GetSelects(SelectExpandClause? clause)
    {
        if (clause is null)
            return new List<string>();

        // TODO: - This may have to be changed!
        return clause.SelectedItems
            .OfType<PathSelectItem>()
            .Select(item => item.SelectedPath.FirstSegment.Identifier)
            .ToList();
    }
}
