#nullable enable

using LogicBuilder.Expressions.Utils.Expansions;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq;
using LogicBuilder.Expressions.Utils;
using System.Diagnostics;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.OData.Query.Expressions;

namespace AutoMapper.AspNet.OData;

internal static class ExpressionUtils
{
    public static ICollection<Expression<Func<TSource, object>>> BuildIncludes<TSource>(
        this IEnumerable<List<Expansion>> includes, List<string> selects)
            where TSource : class
    {
        var expansions = GetAllExpansions(new List<LambdaExpression>());
        return expansions;

        List<Expression<Func<TSource, object>>> GetAllExpansions(List<LambdaExpression> literalOrComplexMemberSelectors)
        {
            string parameterName = "i";
            ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

            literalOrComplexMemberSelectors.AddSelectors(selects, param, param);

            return includes
                .Select(include => BuildSelectorExpression<TSource>(include, literalOrComplexMemberSelectors, parameterName))
                .Concat(literalOrComplexMemberSelectors.Select(selector => (Expression<Func<TSource, object>>)selector))
                .ToList();
        }
    }

    private static Expression GetSelectExpression(IEnumerable<Expansion> expansions, Expression parent, List<LambdaExpression> valueMemberSelectors, string parameterName)
    {
        ParameterExpression parameter = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
        Expression selectorBody = BuildSelectorExpression(parameter, expansions.ToList(), valueMemberSelectors, parameter.Name);
        return Expression.Call
        (
            typeof(Enumerable),
            "Select",
            new Type[] { parameter.Type, selectorBody.Type },
            parent,
            Expression.Lambda
            (
                typeof(Func<,>).MakeGenericType(new[] { parameter.Type, selectorBody.Type }),
                selectorBody,
                parameter
            )
        );
    }

    private static Expression BuildSelectorExpression(Expression sourceExpression, List<Expansion> parts, List<LambdaExpression> valueMemberSelectors, string parameterName = "i")
    {
        Expression parent = sourceExpression;

        //Arguments to create a nested expression when the parent expansion is a collection
        //See AddChildSeelctors() below
        List<LambdaExpression> childValueMemberSelectors = new();

        for (int i = 0; i < parts.Count; i++)
        {
            if (parent.Type.IsList())
            {
                Expression selectExpression = GetSelectExpression
                (
                    parts.Skip(i),
                    parent,
                    childValueMemberSelectors,
                    parameterName
                );

                AddChildSelectors();

                return selectExpression;
            }
            else
            {
                parent = Expression.MakeMemberAccess(parent, parent.Type.GetMemberInfo(parts[i].MemberName));

                if (parent.Type.IsList())
                {
                    ParameterExpression childParam = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
                    //selectors from an underlying list element must be added here.
                    childValueMemberSelectors.AddSelectors
                    (
                        parts[i].Selects,
                        childParam,
                        childParam
                    );
                }
                else
                {
                    valueMemberSelectors.AddSelectors(parts[i].Selects, Expression.Parameter(sourceExpression.Type, parameterName), parent);
                }
            }
        }

        AddChildSelectors();

        return parent;

        //Adding childValueMemberSelectors created above and in a the recursive call:
        //i0 => i0.Builder.Name becomes
        //i => i.Buildings.Select(i0 => i0.Builder.Name)
        void AddChildSelectors()
        {
            childValueMemberSelectors.ForEach(selector =>
            {
                valueMemberSelectors.Add(Expression.Lambda
                (
                    typeof(Func<,>).MakeGenericType(new[] { sourceExpression.Type, typeof(object) }),
                    Expression.Call
                    (
                        typeof(Enumerable),
                        "Select",
                        new Type[] { parent.GetUnderlyingElementType(), typeof(object) },
                        parent,
                        selector
                    ),
                    Expression.Parameter(sourceExpression.Type, parameterName)
                ));
            });
        }
    }

    // Builds the the main selector delegate.
    private static Expression<Func<TSource, object>> BuildSelectorExpression<TSource>(List<Expansion> fullName, List<LambdaExpression> valueMemberSelectors, string parameterName = "i")
    {
        ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

        return (Expression<Func<TSource, object>>)Expression.Lambda
        (
            typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
            BuildSelectorExpression(param, fullName, valueMemberSelectors, parameterName),
            param
        );
    }

    private static void AddSelectors(
        this List<LambdaExpression> literalOrComplexMemberSelectors, List<string> selects, ParameterExpression param, Expression parentBody)
    {
        if (parentBody.Type.IsList() || parentBody.Type.IsLiteralType())
            return;

        literalOrComplexMemberSelectors.AddRange
        (
            parentBody.Type
                .GetSelectMembersOrAllLiteralMembers(selects)
                .Select(member => Expression.MakeMemberAccess(parentBody, member))
                .Select
                (
                    selector => selector.Type.IsValueType
                        ? (Expression)Expression.Convert(selector, typeof(object))
                        : selector
                )
                .Select
                (
                    selector => Expression.Lambda
                    (
                        typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
                        selector,
                        param
                    )
                )
        );
    }

    private static string ChildParameterName(this string currentParameterName)
    {
        string lastChar = currentParameterName[^1..];
        if (short.TryParse(lastChar, out short lastCharShort))
        {
            return string.Concat(currentParameterName[..^1], 
                (lastCharShort++).ToString(CultureInfo.CurrentCulture));
        }

        return currentParameterName += "0";
    }
}
