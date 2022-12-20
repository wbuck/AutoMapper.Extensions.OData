#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class TypeExt
{
    public static MemberInfo[] GetSelectMembersOrAllLiteralMembers(this Type parentType, List<string>? selects = null)
    {
        if (selects is null || !selects.Any())
            return parentType.GetLiteralTypeMembers();

        return selects.Select(select => parentType.GetMemberInfo(select)).ToArray();
    }

    public static MemberInfo[] GetPropertiesAndFields(this Type parentType) =>
        parentType.GetMemberInfos()
            .Where(info => 
                info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property)
            .ToArray();

    public static List<List<PathSegment>> GetComplexTypeSelects(this IEdmModel edmModel, Type parentType)
    {
        return GetComplexTypeSelects(new(), new(), parentType, edmModel);
    }

    public static List<List<PathSegment>> GetLiteralSelects(this Type parentType, IEdmModel edmModel, List<PathSegment>? pathSegments = null) =>
        parentType.GetLiteralTypeMembers()
            .Select(member => new List<PathSegment>(pathSegments ?? Enumerable.Empty<PathSegment>())
            {
                new
                (
                    false,
                    member,
                    parentType,
                    member.GetMemberType(),
                    EdmTypeKind.Primitive,
                    edmModel
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

    private static List<List<PathSegment>> GetComplexTypeSelects(
        List<List<PathSegment>> expansions, 
        List<PathSegment> currentExpansions,
        Type parentType,
        IEdmModel edmModel, 
        int depth = 0)
    {
        var members = edmModel.GetComplexMembers(parentType);

        for (int i = 0; i < members.Count; ++i)
        {
            var member = members[i];
            Type memberType = member.GetMemberType();                

            List<PathSegment> pathSegments = i == 0 ? currentExpansions : new(currentExpansions.Take(depth));
            pathSegments.Add(new PathSegment
            (
                false,
                member,
                parentType,
                memberType,
                EdmTypeKind.Complex,
                edmModel
            ));

            Type elementType = pathSegments.Last().ElementType;
            var memberSelects = elementType.GetLiteralSelects(edmModel, pathSegments);

            if (memberSelects.Any())
                expansions.AddRange(memberSelects);

            GetComplexTypeSelects(expansions, pathSegments, elementType, edmModel, depth + 1);
        }

        return expansions;
    }

    private static IReadOnlyList<MemberInfo> GetComplexMembers(this IEdmModel edmModel, Type parentType)
    {
        MemberInfo[] members = parentType.GetPropertiesAndFields();
        List<MemberInfo> complexMembers = new(members.Length);

        var complexTypes = edmModel.SchemaElements.OfType<IEdmComplexType>();

        foreach (var member in members)
        {
            var memberType = member.GetMemberType().GetCurrentType();

            if (!member.IsListOfLiteralTypes() && !memberType.IsLiteralType() &&
                complexTypes.Any(c => c.Name.Equals(memberType.Name, StringComparison.Ordinal)))
            {
                complexMembers.Add(member);
            }
        }
        return complexMembers;
    }

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
