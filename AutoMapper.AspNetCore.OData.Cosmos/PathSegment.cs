#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal readonly struct PathSegment
{
    private readonly SelectItem selectItem;
    private readonly ODataPathSegment pathSegment;

    public PathSegment(SelectItem selectItem, ODataPathSegment pathSegment)
    {
        this.selectItem = selectItem;
        this.pathSegment = pathSegment;
    }

    public string Identifier => 
        this.pathSegment.Identifier;

    public bool IsNavigationPropertySegment =>
        this.pathSegment is NavigationPropertySegment;

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

    public List<string> GetSelects()
    {
        if (TryGetExpandedNavigationSelectItem(out var item))
            return GetSelects(item.SelectAndExpand);

        return new();
    }

    public bool TryGetExpandedNavigationSelectItem([MaybeNullWhen(false)] out ExpandedNavigationSelectItem item)
    {
        if (this.pathSegment is NavigationPropertySegment 
            && this.selectItem is ExpandedNavigationSelectItem navItem)
        {
            item = navItem;
            return true;
        }
        item = null;
        return false;
    }

    public static implicit operator ODataPathSegment(PathSegment pathSegment) => 
        pathSegment.pathSegment;

    private static List<string> GetSelects(SelectExpandClause clause)
    {
        if (clause == null)
            return new List<string>();

        return clause.SelectedItems
            .OfType<PathSelectItem>()
            .Select(item => item.SelectedPath.FirstSegment.Identifier)
            .ToList();
    }
}
