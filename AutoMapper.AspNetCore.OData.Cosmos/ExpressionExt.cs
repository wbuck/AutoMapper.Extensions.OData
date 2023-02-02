#nullable enable

using AutoMapper.AspNet.OData.Visitors;
using AutoMapper.Internal;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData;
internal static class ExpressionExt
{   
    public static Expression? GetQueryableExpression(this Expression expression, IReadOnlyList<PathSegment> pathSegments, QueryOptions options, ODataQueryContext context)
        => QueryMethodInserter.Insert(pathSegments, options, context, expression);

    public static LambdaExpression GetSelector(this OrderByClause clause, IReadOnlyList<PathSegment> pathSegments)
    {
        Type elementType = pathSegments[0].ElementType;
        ParameterExpression parameter = Expression.Parameter(elementType, clause.RangeVariable.Name.Replace("$", string.Empty));

        Expression memberExpression = pathSegments.Skip(1).Aggregate((Expression)parameter, (expression, next) 
            => Expression.MakeMemberAccess(expression, next.Member));

        string[] properties = clause.Expression.GetPropertyPath().Split('.');

        memberExpression = properties.Aggregate(memberExpression, (expression, next) 
            => Expression.MakeMemberAccess(expression, expression.Type.GetFieldOrProperty(next)));

        return Expression.Lambda
        (
            memberExpression,
            parameter
        );
    }        

    private static string GetPropertyPath(this SingleValueNode node) => node switch
    {
        CountNode countNode => countNode.GetPropertyPath(),
        _ => ((SingleValuePropertyAccessNode)node).GetPropertyPath()
    };
}
