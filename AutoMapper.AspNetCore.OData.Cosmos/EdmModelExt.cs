#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;
internal static class EdmModelExt
{
    public static List<List<PathSegment>> GetComplexTypeSelects(this IEdmModel edmModel, Type parentType) =>
       GetComplexTypeSelects(new(), new(), parentType, edmModel);

    private static List<List<PathSegment>> GetComplexTypeSelects(
        List<List<PathSegment>> expansions,
        List<PathSegment> currentExpansions,
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
                ? currentExpansions
                : new(currentExpansions.Take(depth));

            pathSegments.Add(new PathSegment
            (
                member,
                parentType,
                memberType,
                EdmTypeKind.Complex
            ));

            Type elementType = pathSegments.Last().ElementType;
            var memberSelects = elementType.GetLiteralSelects(pathSegments);

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
}
