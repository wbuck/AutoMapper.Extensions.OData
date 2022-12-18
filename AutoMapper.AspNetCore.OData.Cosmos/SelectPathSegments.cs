#nullable enable

using Microsoft.Azure.Cosmos.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal readonly struct SelectPathSegments : IEquatable<SelectPathSegments>, IEnumerable<SelectPathSegment>
{
    private readonly SelectItem selectItem;
    private readonly IReadOnlyCollection<SelectPathSegment> pathSegments;
    private readonly string rawPath;

    public SelectPathSegments(SelectItem selectItem)
        : this (selectItem, GetPathSegments(selectItem).ToArray())
    {}

    public SelectPathSegments(SelectItem selectItem, IReadOnlyCollection<SelectPathSegment> pathSegments)
    {
        this.selectItem = selectItem;
        this.pathSegments = pathSegments;
        this.rawPath = string.Join('/', this.pathSegments.Select(s => s.Identifier));
    }

    public string RawPath => 
        this.rawPath;

    public bool IsExpandedNavigationSelectItem() =>
        this.selectItem is ExpandedNavigationSelectItem;

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
            PathSelectItem item => BuildSelect(item),
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty.Select(s => new SelectPathSegment(item, s)),
            _ => throw new NotSupportedException()
        };

    private static IReadOnlyCollection<SelectPathSegment> BuildSelect(PathSelectItem item)
    {
        var pathCount = item.SelectedPath.Count;

        if (pathCount < 2 || !TryGetSelect(item.SelectedPath.Last(), out var select))        
            return item.SelectedPath.Select(path => new SelectPathSegment(item, path)).ToArray();

        
        var odataSegment = item.SelectedPath.Take(pathCount - 1).ToArray();
        var pathSegments = new List<SelectPathSegment>(pathCount - 1);

        for (int i = 0; i < odataSegment.Length; ++i)
        {
            int count = i + 1;
            pathSegments.Add(new SelectPathSegment(item, odataSegment[i], count == odataSegment.Length ? new[] { select } : null));
        }
        
        return pathSegments;

        static bool TryGetSelect(ODataPathSegment segment, [MaybeNullWhen(false)] out string select)
        {
            select = null;
            if (segment.EdmType.TypeKind == EdmTypeKind.Primitive ||
                segment.EdmType.TypeKind == EdmTypeKind.Enum)
            {
                select = segment.Identifier;
            }
            
            return select is not null;
        }
    }

    public IEnumerator<SelectPathSegment> GetEnumerator() =>
        this.pathSegments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this.GetEnumerator();
    
}
