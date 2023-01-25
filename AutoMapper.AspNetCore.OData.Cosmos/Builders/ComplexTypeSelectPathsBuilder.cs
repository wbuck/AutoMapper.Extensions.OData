#nullable enable

using AutoMapper;
using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class ComplexTypeSelectPathsBuilder
{
    public static List<List<PathSegment>> GetValueAndComplexMemberSelects(this Type parentType, IEdmModel edmModel) =>
        parentType.GetValueTypeMembersSelects().Concat(edmModel.GetComplexTypeSelects(parentType)).ToList();

    public static List<List<PathSegment>> GetValueTypeMembersSelects(this Type parentType, List<PathSegment>? pathSegments = null) =>
        parentType.GetValueOrListOfValueTypeMembers()
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

    public static List<List<PathSegment>> GetComplexTypeSelects(this IEdmModel edmModel, Type parentType) =>
       GetComplexTypeSelects(new(), new(), parentType, edmModel);

    private static List<List<PathSegment>> GetComplexTypeSelects(
        List<List<PathSegment>> selects,
        List<PathSegment> currentSelects,
        Type parentType,
        IEdmModel edmModel,
        in int depth = 0)
    {
        var members = edmModel.GetComplexMembers(parentType);

        for (int i = 0; i < members.Count; ++i)
        {
            MemberInfo member = members[i];
            Type memberType = member.GetMemberType();

            List<PathSegment> pathSegments = i == 0
                ? currentSelects
                : new(currentSelects.Take(depth));

            pathSegments.Add(new PathSegment
            (
                member,
                parentType,
                memberType,
                EdmTypeKind.Complex
            ));

            Type elementType = pathSegments.Last().ElementType;
            List<List<PathSegment>> memberSelects = elementType.GetValueTypeMembersSelects(pathSegments);

            if (memberSelects.Any())
                selects.AddRange(memberSelects);
            else
                selects.Add(new(pathSegments));

            GetComplexTypeSelects(selects, pathSegments, elementType, edmModel, depth + 1);
        }

        return selects;
    }

    private static IReadOnlyList<MemberInfo> GetComplexMembers(this IEdmModel edmModel, Type parentType)
    {
        MemberInfo[] members = parentType.GetFieldsAndProperties();
        List<MemberInfo> complexMembers = new(members.Length);

        var complexTypes = edmModel.SchemaElements.OfType<IEdmComplexType>();

        foreach (var member in members)
        {
            Type memberType = member.GetMemberType().GetCurrentType();

            if (!member.IsListOfValueTypes() && !memberType.IsLiteralType() &&
                complexTypes.Any(c => c.Name.Equals(memberType.Name, StringComparison.Ordinal)))
            {
                complexMembers.Add(member);
            }
        }
        return complexMembers;
    }
}
