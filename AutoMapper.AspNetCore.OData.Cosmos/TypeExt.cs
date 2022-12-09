#nullable enable

using AutoMapper.Execution;
using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class TypeExt
{
    public static MemberInfo[] GetSelectMembersOrAllLiteralMembers(this Type parentType, List<string> selects)
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
    public static List<List<ODataExpansionOptions>> ExpandComplexTypes(this Type parentType, IEnumerable<string> complexTypeNames)
    {
        List<List<ODataExpansionOptions>> expansions = new();
        return ExpandComplexInternal(expansions, new(), parentType, complexTypeNames);

        static List<List<ODataExpansionOptions>> ExpandComplexInternal(List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions> currentExpansions,
            Type parentType, IEnumerable<string> complexTypeNames, int depth = 0)
        {
            var members = parentType.GetComplexMembers(complexTypeNames);

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
        }

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

    private static MemberInfo[] GetLiteralTypeMembers(this Type parentType)
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
