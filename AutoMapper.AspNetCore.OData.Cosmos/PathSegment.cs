#nullable enable

using LogicBuilder.Expressions.Utils;
using LogicBuilder.Expressions.Utils.Expansions;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

public sealed class PathSegment : ODataExpansionOptions
{
    public PathSegment(
        bool isExpansionSegment,
        MemberInfo member,
        Type parentType,
        Type memberType,
        EdmTypeKind edmTypeKind,
        IEdmModel edmModel,
        FilterOptions? filterOptions = null,
        QueryOptions? queryOptions = null,
        List<List<PathSegment>>? selectPaths = null)
    {
        IsExpansionSegment = isExpansionSegment;
        Member = member;
        MemberName = member.Name;
        ParentType = parentType;
        MemberType = memberType;
        ElementType = memberType.GetCurrentType();
        EdmTypeKind = edmTypeKind;
        EdmModel = edmModel;
        FilterOptions = filterOptions!;
        QueryOptions = queryOptions!;
        SelectPaths = selectPaths;
        IsCollection = memberType.IsList();
    }

    public bool IsExpansionSegment { get; }
    public MemberInfo Member { get; }
    public Type ElementType { get; }
    public EdmTypeKind EdmTypeKind { get; }
    public IEdmModel EdmModel { get; }
    public IReadOnlyList<IReadOnlyList<PathSegment>>? SelectPaths { get; }
    public bool IsComplex => EdmTypeKind == EdmTypeKind.Complex;
    public bool IsEntity => EdmTypeKind == EdmTypeKind.Entity;
    public bool IsBasic => EdmTypeKind == EdmTypeKind.Primitive || EdmTypeKind == EdmTypeKind.Enum;
    public bool IsCollection { get; }
}
