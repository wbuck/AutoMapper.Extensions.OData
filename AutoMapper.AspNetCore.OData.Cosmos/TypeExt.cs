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
    public static List<List<PathSegment>> GetLiteralAndComplexSelects(this Type parentType, IEdmModel edmModel) =>
        parentType.GetLiteralSelects().Concat(edmModel.GetComplexTypeSelects(parentType)).ToList();

    public static List<List<PathSegment>> GetLiteralSelects(this Type parentType, List<PathSegment>? pathSegments = null) =>
        parentType.GetLiteralTypeMembers()
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

    public static MemberInfo[] GetLiteralTypeMembers(this Type parentType)
    {
        if (parentType.IsList())
            return Array.Empty<MemberInfo>();

        return parentType.GetMemberInfos().Where
        (
            info =>
               (info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property) &&
               (info.GetMemberType().IsLiteralType() || info.IsListOfLiteralTypes())
        ).ToArray();
    }

    public static MemberInfo[] GetPropertiesAndFields(this Type parentType) =>
        parentType.GetMemberInfos()
            .Where(info =>
                info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property)
            .ToArray();

    public static bool IsListOfLiteralTypes(this MemberInfo memberInfo)
    {
        var memberType = memberInfo.GetMemberType();
        return memberType.IsListOfLiteralTypes();
    }

    public static bool IsListOfLiteralTypes(this Type type) =>
        type.IsList() && type.GetUnderlyingElementType().IsLiteralType();


    private static MemberInfo[] GetMemberInfos(this Type parentType)
        => parentType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase);
}
