#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal record struct PathSegment
{
    public PathSegment(
        MemberInfo member,
        Type parentType,
        Type memberType,
        EdmTypeKind edmTypeKind,
        FilterOptions? filterOptions = null,
        QueryOptions? queryOptions = null,
        List<List<PathSegment>>? selectPaths = null)
    {
        Member = member;
        MemberName = member.Name;
        ParentType = parentType;
        MemberType = memberType;
        ElementType = memberType.GetCurrentType();
        EdmTypeKind = edmTypeKind;
        FilterOptions = filterOptions;
        QueryOptions = queryOptions;
        SelectPaths = selectPaths;
        IsCollection = memberType.IsList();
    }

    public string MemberName { get; }
    public Type MemberType { get; }
    public Type ParentType { get; }
    public MemberInfo Member { get; }
    public Type ElementType { get; }
    public EdmTypeKind EdmTypeKind { get; }
    public FilterOptions? FilterOptions { get; }
    public QueryOptions? QueryOptions { get; }
    public List<List<PathSegment>>? SelectPaths { get; }
    public bool IsComplex => EdmTypeKind == EdmTypeKind.Complex;
    public bool IsEntity => EdmTypeKind == EdmTypeKind.Entity;
    public bool IsLiteral => EdmTypeKind == EdmTypeKind.Primitive || EdmTypeKind == EdmTypeKind.Enum;
    public bool IsCollection { get; }
}
