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

    public static MemberInfo[] GetPropertiesOrFields(this Type parentType) =>
        parentType.GetMemberInfos()
            .Where(info => 
                info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property)
            .ToArray();

    public static bool IsComplexType(this Type type, IEnumerable<string> complexTypeNames) =>
        complexTypeNames.Contains(type.Name, StringComparer.Ordinal);

    public static IEdmComplexType? GetComplexType(this IEdmModel edmModel, Type rootType) =>
        edmModel.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name.Equals(rootType.Name, StringComparison.Ordinal));

    public static IReadOnlyList<MemberInfo> GetComplexMembers(this IEdmModel edmModel, Type parentType)
    {
        MemberInfo[] members = parentType.GetPropertiesOrFields();
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

    public static IReadOnlyList<MemberInfo> GetComplexMembers(this Type type, IEnumerable<string> complexTypeNames, IReadOnlyList<string>? selects = null)
    {
        MemberInfo[] members = type.GetPropertiesOrFields();
        List<MemberInfo> complexMembers = new(members.Length);

        foreach (var member in members)
        {
            if (selects is not null && selects.Any() && !selects.Contains(member.Name))
                continue;

            var memberType = member.GetMemberType().GetCurrentType();

            if (!member.IsListOfLiteralTypes() && !memberType.IsLiteralType() &&
                complexTypeNames.Contains(memberType.Name, StringComparer.Ordinal))
            {
                complexMembers.Add(member);
            }
        }
        return complexMembers;
    }
    
    /// <summary>
    /// Creates include paths to each complex type (as defined by the EDM model).
    /// </summary>
    /// <remarks>   
    /// Given the classes below with the "parentType" set to typeof(Entity)
    /// the following include paths will be returned:
    /// 
    /// [0]: [0]: FirstComplex, [1]: SecondComplex, [2]: ThirdComplex
    /// [1]: [0]: FirstComplex, [1]: FourthComplex    
    /// 
    /// The first segment of both paths (FirstComplex) is the "connection"
    /// to the "parentType".
    /// </remarks>
    /// 
    /// <example>        
    /// <![CDATA[
    /// class FourthComplex {}
    /// 
    /// class ThirdComplex {}
    /// 
    /// class SecondComplex
    /// {
    ///     public ThirdComplex ThirdComplex {get; set;} 
    /// }
    /// 
    /// class FirstComplex
    /// {
    ///     public SecondComplex SecondComplex {get; set;}
    ///     public FourthComplex FourthComplex {get; set;}
    /// }
    /// 
    /// class Entity 
    /// {
    ///     public Guid Id {get; set;}
    ///     public FirstComplex FirstComplex {get; set;}
    /// }
    /// ]]>
    /// </example>
    public static List<List<ODataExpansionOptions>> ExpandComplexTypes(this Type parentType, IEnumerable<string> complexTypeNames, IEnumerable<string> ignoredProperties) =>
        ExpandComplexInternal(new(), new(), parentType, complexTypeNames, ignoredProperties: ignoredProperties);

    private static List<List<ODataExpansionOptions>> ExpandComplexInternal(
        List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions> currentExpansions, 
        Type parentType, IEnumerable<string> complexTypeNames, int depth = 0, IEnumerable<string>? ignoredProperties = null)
    {
        var members = parentType.GetComplexMembers(complexTypeNames);

        if (ignoredProperties is not null)
        {
            members = members.Where(m => !ignoredProperties.Contains(m.Name)).ToList();
        }

        for (int i = 0; i < members.Count; ++i)
        {
            var member = members[i];
            var memberType = member.GetMemberType().GetCurrentType();

            var nextExpansions = AddExpansion
            (
                member, memberType, i == 0 ? currentExpansions : new(currentExpansions.Take(depth))
            );

            ExpandComplexInternal(expansions, nextExpansions, memberType, complexTypeNames, depth + 1);

            if (!nextExpansions.Equals(currentExpansions) || depth == 0)
                expansions.Add(nextExpansions);
        }

        return expansions;

        static List<ODataExpansionOptions> AddExpansion(MemberInfo member, Type memberType, List<ODataExpansionOptions> expansions)
        {
            expansions.Add(new()
            {
                MemberName = member.Name,
                MemberType = memberType,
                ParentType = member.DeclaringType,
                Selects = new()
            });
            return expansions;
        }
    }


#if true
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
                    member.Name,
                    parentType,
                    member.GetMemberType(),
                    EdmTypeKind.Primitive,
                    edmModel
                )
            }).ToList();
    

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
                member.Name,
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

#endif

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

    private static bool IsListOfLiteralTypes(this MemberInfo memberInfo)
    {
        var memberType = memberInfo.GetMemberType();

        if (!memberType.IsList())
            return false;

        return memberType.GetUnderlyingElementType().IsLiteralType();
    }

    private static MemberInfo[] GetMemberInfos(this Type parentType)
        => parentType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase);
}
