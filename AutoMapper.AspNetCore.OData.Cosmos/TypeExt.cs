#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static class TypeExt
{
    private const BindingFlags InstanceFlags = 
        BindingFlags.Public | 
        BindingFlags.Instance | 
        BindingFlags.FlattenHierarchy | 
        BindingFlags.IgnoreCase;

    public static MemberInfo[] GetValueOrListOfValueTypeMembers(this Type parentType)
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

    public static LambdaExpression GetTypedSelector(this Type parentType, IEnumerable<PathSegment> pathSegments, in string parameterName = "a")
    {
        ParameterExpression param = Expression.Parameter(parentType, parameterName);

        Expression body = pathSegments.Aggregate((Expression)param, (expression, next) => 
            Expression.MakeMemberAccess(expression, next.Member));

        Type[] typeArgs = new[] { parentType, body.Type };
        Type delegateType = typeof(Func<,>).MakeGenericType(typeArgs);
        return Expression.Lambda(delegateType, body, param);
    }

    private static MemberInfo[] GetMemberInfos(this Type parentType)
        => parentType.GetMembers(InstanceFlags);
}
