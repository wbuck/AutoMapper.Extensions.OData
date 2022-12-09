#nullable enable

using Microsoft.OData.UriParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal readonly struct SelectPathSegments : IEquatable<SelectPathSegments>, IEnumerable<SelectPathSegment>
{
    private readonly SelectItem selectItem;
    private readonly IReadOnlyCollection<SelectPathSegment> pathSegments;
    private readonly string rawPath;

    public SelectPathSegments(SelectItem selectItem)
    {
        this.selectItem = selectItem;
        this.pathSegments = GetPathSegments(this.selectItem).ToArray();
        this.rawPath = string.Join('/', this.pathSegments.Select(s => s.Identifier));
    }

    public override bool Equals(object? obj) =>
        obj is SelectPathSegments segments && Equals(segments);

    public override int GetHashCode() =>
        this.rawPath.GetHashCode();

    public bool Equals(SelectPathSegments other) =>
        this.rawPath.Equals(other.rawPath, StringComparison.Ordinal);

    public IImmutableSet<SelectPathSegments> GetNestedExpansionSegments()
    {                
        if (this.selectItem is ExpandedNavigationSelectItem navigationSelectItem)
        {
            return navigationSelectItem.SelectAndExpand.SelectedItems
                .OrderByDescending(s => s is ExpandedNavigationSelectItem)
                .Select(s => new SelectPathSegments(s))
                .ToImmutableHashSet();
        }
        return ImmutableHashSet.Create<SelectPathSegments>();
    }

    public static bool operator ==(SelectPathSegments lhs, SelectPathSegments rhs) =>
        lhs.Equals(rhs);

    public static bool operator !=(SelectPathSegments lhs, SelectPathSegments rhs) =>
        !(lhs == rhs);

    private static IEnumerable<SelectPathSegment> GetPathSegments(SelectItem selectItem) =>
        selectItem switch
        {
            PathSelectItem item => item.SelectedPath.Select(s => new SelectPathSegment(item, s)),
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty.Select(s => new SelectPathSegment(item, s)),
            _ => throw new NotSupportedException()
        };

    public IEnumerator<SelectPathSegment> GetEnumerator() =>
        this.pathSegments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this.GetEnumerator();
    
}
