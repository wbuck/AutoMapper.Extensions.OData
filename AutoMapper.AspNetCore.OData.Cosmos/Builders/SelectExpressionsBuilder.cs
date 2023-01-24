#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq;
using LogicBuilder.Expressions.Utils;
using AutoMapper.Internal;


namespace AutoMapper.AspNet.OData;

internal static class SelectExpressionsBuilder
{
    public static ICollection<Expression<Func<TSource, object>>> BuildSelectExpressions<TSource>(
        this IEnumerable<List<PathSegment>> includes, List<List<PathSegment>> selects)
            where TSource : class
    {
        var expansions = GetAllExpansions(new List<LambdaExpression>());
        return expansions;

        List<Expression<Func<TSource, object>>> GetAllExpansions(List<LambdaExpression> selectors)
        {
            const string parameterName = "i";
            ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

            selectors.AddSelectors(selects, param, param);

            return includes
                .Select(include => BuildSelectorExpression<TSource>(include, selectors, parameterName))
                .Concat(selectors.Select(selector => (Expression<Func<TSource, object>>)selector))
                .ToList();
        }
    }

    private static Expression GetSelectExpression(IEnumerable<PathSegment> expansions, Expression parent, List<LambdaExpression> memberSelectors, string parameterName)
    {
        ParameterExpression parameter = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
        Expression selectorBody = BuildSelectorExpression(parameter, expansions.ToList(), memberSelectors, parameter.Name!);
        return Expression.Call
        (
            LinqMethods.EnumerableSelectMethod.MakeGenericMethod(parameter.Type, selectorBody.Type),
            parent,
            Expression.Lambda
            (
                typeof(Func<,>).MakeGenericType(new[] { parameter.Type, selectorBody.Type }),
                selectorBody,
                parameter
            )
        );
    }

    private static Expression BuildSelectorExpression(Expression sourceExpression, List<PathSegment> expansionPath, List<LambdaExpression> memberSelectors, string parameterName = "i")
    {
        Expression parent = sourceExpression;

        //Arguments to create a nested expression when the parent expansion is a collection
        //See AddChildSeelctors() below
        List<LambdaExpression> childValueMemberSelectors = new();

        for (int i = 0; i < expansionPath.Count; i++)
        {
            if (parent.Type.IsList())
            {
                Expression selectExpression = GetSelectExpression
                (
                    expansionPath.Skip(i),
                    parent,
                    childValueMemberSelectors,
                    parameterName
                );

                AddChildSelectors();

                return selectExpression;
            }
            else
            {
                parent = Expression.MakeMemberAccess(parent, expansionPath[i].Member);

                if (parent.Type.IsList())
                {
                    ParameterExpression childParam = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
                    //selectors from an underlying list element must be added here.
                    childValueMemberSelectors.AddSelectors
                    (
                        expansionPath[i].SelectPaths,
                        childParam,
                        childParam
                    );
                }
                else
                {
                    memberSelectors.AddSelectors
                    (
                        expansionPath[i].SelectPaths, 
                        Expression.Parameter(sourceExpression.Type, parameterName), 
                        parent
                    );
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
                memberSelectors.Add(Expression.Lambda
                (
                    typeof(Func<,>).MakeGenericType(new[] { sourceExpression.Type, typeof(object) }),
                    Expression.Call
                    (
                        LinqMethods.EnumerableSelectMethod.MakeGenericMethod(parent.GetUnderlyingElementType(), typeof(object)),
                        parent,
                        selector
                    ),
                    Expression.Parameter(sourceExpression.Type, parameterName)
                ));
            });
        }
    }

    // Builds the the main selector delegate.
    private static Expression<Func<TSource, object>> BuildSelectorExpression<TSource>(List<PathSegment> expansionPath, List<LambdaExpression> memberSelectors, string parameterName = "i")
    {
        ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

        return (Expression<Func<TSource, object>>)Expression.Lambda
        (
            typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
            BuildSelectorExpression(param, expansionPath, memberSelectors, parameterName),
            param
        );
    }

    private static void AddSelectors(
        this List<LambdaExpression> selectors, 
        IReadOnlyList<IReadOnlyList<PathSegment>>? selects, 
        ParameterExpression param, 
        Expression parentBody)
    {
        if (parentBody.Type.IsList() || parentBody.Type.IsLiteralType() || selects is null)
            return;

        selectors.AddRange
        (
            selects
                .Select(path => path.GetSelects(parentBody))
                .Select(expression => Expression.Lambda
                (
                    typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
                    expression,
                    param
                ))
        );
    }

    private static Expression GetSelects(this IReadOnlyList<PathSegment> pathSegments, Expression expression)
    {
        if (!pathSegments.Any())
            return expression;

        Type parentType = expression.Type;

        for (int i = 0; i < pathSegments.Count; i++)
        {
            if (!parentType.IsList() || parentType.IsListOfValueTypes())
            {
                MemberExpression memberExpression = Expression.MakeMemberAccess
                (
                    expression, 
                    pathSegments[i].Member
                );

                expression = memberExpression.Type.IsValueType
                    ? Expression.Convert(memberExpression, typeof(object))
                    : memberExpression;

                parentType = pathSegments[i].MemberType;
            }
            else
            {
                Type elementType = parentType.GetUnderlyingElementType();
                ParameterExpression parameter = Expression.Parameter(elementType, "i1");

                MemberExpression memberExpression = Expression.MakeMemberAccess
                (
                    parameter, 
                    pathSegments[i].Member
                );

                LambdaExpression lambda = Expression.Lambda
                (
                    pathSegments.Skip(i + 1).ToList().GetSelects(memberExpression),
                    parameter
                );

                expression = Expression.Call
                (
                    LinqMethods.EnumerableSelectMethod.MakeGenericMethod(elementType, lambda.ReturnType),
                    expression,
                    lambda
                );

                break;
            }

        }
        return expression;
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
