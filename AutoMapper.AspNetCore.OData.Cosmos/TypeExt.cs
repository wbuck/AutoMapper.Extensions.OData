﻿#nullable enable

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
