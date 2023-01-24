#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class TypeExt
{
    private const BindingFlags InstanceFlags = 
        BindingFlags.Public | 
        BindingFlags.Instance | 
        BindingFlags.FlattenHierarchy | 
        BindingFlags.IgnoreCase;

    public static List<List<PathSegment>> GetValueAndComplexMemberSelects(this Type parentType, IEdmModel edmModel) =>
        parentType.GetValueTypeMembersSelects().Concat(edmModel.GetComplexTypeSelects(parentType)).ToList();

    public static List<List<PathSegment>> GetValueTypeMembersSelects(this Type parentType, List<PathSegment>? pathSegments = null) =>
        parentType.GetValueOrListOrValueTypeMembers()
            .Select(member => new List<PathSegment>(pathSegments ?? Enumerable.Empty<PathSegment>())
            {
                new
                (
                    member,
                    parentType,
                    member.GetMemberType(),
                    EdmTypeKind.Primitive
                )
            }).ToList();

    public static MemberInfo[] GetValueOrListOrValueTypeMembers(this Type parentType)
    {
        if (parentType.IsList())
            return Array.Empty<MemberInfo>();

        return parentType.GetMemberInfos().Where
        (
            info =>
               (info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property) &&
               (info.GetMemberType().IsLiteralType() || info.IsListOfValueTypes())
        ).ToArray();
    }

    public static MemberInfo[] GetFieldsAndProperties(this Type parentType) =>
        parentType.GetMemberInfos()
            .Where(info => info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property)
            .ToArray();      

    private static MemberInfo[] GetMemberInfos(this Type parentType)
        => parentType.GetMembers(InstanceFlags);
}
