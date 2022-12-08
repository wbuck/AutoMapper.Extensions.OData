#nullable enable

using Microsoft.OData.UriParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal readonly struct ExpansionSegments : IEquatable<ExpansionSegments>, IEnumerable<PathSegment>
{
    private readonly SelectItem selectItem;
    private readonly IReadOnlyCollection<PathSegment> pathSegments;
    private readonly string rawPath;

    public ExpansionSegments(SelectItem selectItem)
    {
        this.selectItem = selectItem;
        this.pathSegments = GetPathSegments(this.selectItem).ToArray();
        this.rawPath = string.Join('/', this.pathSegments.Select(s => s.Identifier));
    }

    public override bool Equals(object? obj) =>
        obj is ExpansionSegments segments && Equals(segments);

    public override int GetHashCode() =>
        this.rawPath.GetHashCode();

    public bool Equals(ExpansionSegments other) =>
        this.rawPath.Equals(other.rawPath, StringComparison.Ordinal);

    public static implicit operator ExpandedNavigationSelectItem?(ExpansionSegments segments) =>
        segments.selectItem as ExpandedNavigationSelectItem;

    public static bool operator ==(ExpansionSegments lhs, ExpansionSegments rhs) =>
        lhs.Equals(rhs);

    public static bool operator !=(ExpansionSegments lhs, ExpansionSegments rhs) =>
        !(lhs == rhs);

    private static IEnumerable<PathSegment> GetPathSegments(SelectItem selectItem) =>
        selectItem switch
        {
            PathSelectItem item => item.SelectedPath.Select(s => new PathSegment(item, s)),
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty.Select(s => new PathSegment(item, s)),
            _ => throw new NotSupportedException()
        };

    public IEnumerator<PathSegment> GetEnumerator() =>
        this.pathSegments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this.GetEnumerator();
    
}
