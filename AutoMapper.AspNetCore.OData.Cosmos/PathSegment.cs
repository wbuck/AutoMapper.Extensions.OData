#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;

namespace AutoMapper.AspNet.OData;

public sealed record PathSegment
{
    public PathSegment(
        bool isExpansionSegment,
        string memberName,
        Type parentType,
        Type memberType,
        EdmTypeKind edmTypeKind,
        IEdmModel edmModel,
        FilterOptions? filterOptions = null,
        QueryOptions? queryOptions = null,
        List<List<PathSegment>>? selects = null)
    {
        IsExpansionSegment = isExpansionSegment;
        MemberName = memberName;
        ParentType = parentType;
        MemberType = memberType;
        ElementType = memberType.GetCurrentType();
        EdmTypeKind = edmTypeKind;
        EdmModel = edmModel;
        FilterOptions = filterOptions;
        QueryOptions = queryOptions;
        Selects = selects;
        IsCollection = memberType.IsList();
    }

    public bool IsExpansionSegment { get; }
    public string MemberName { get; }
    public Type ParentType { get; }
    public Type MemberType { get; }
    public Type ElementType { get; }
    public EdmTypeKind EdmTypeKind { get; }
    public IEdmModel EdmModel { get; }
    public FilterOptions? FilterOptions { get; }
    public QueryOptions? QueryOptions { get; }
    public IReadOnlyList<IReadOnlyList<PathSegment>>? Selects { get; }
    public bool IsComplex => EdmTypeKind == EdmTypeKind.Complex;
    public bool IsEntity => EdmTypeKind == EdmTypeKind.Entity;
    public bool IsBasic => EdmTypeKind == EdmTypeKind.Primitive || EdmTypeKind == EdmTypeKind.Enum;
    public bool IsCollection { get; }
}
