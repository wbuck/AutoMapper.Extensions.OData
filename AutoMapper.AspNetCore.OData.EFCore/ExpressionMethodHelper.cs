#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper.AspNet.OData;
internal class ExpressionMethodHelper
{
    private static readonly MethodInfo _queryableWhereMethod =
        GetGenericMethod(_ => Queryable.Where(default(IQueryable<int>)!, default(Expression<Func<int, bool>>)!));

    private static readonly MethodInfo _queryableSelectMethod =
        GetGenericMethod(_ => Queryable.Select(default(IQueryable<int>)!, i => i));

    private static readonly MethodInfo _enumerableSelectMethod =
        GetGenericMethod(_ => Enumerable.Select(default(IEnumerable<int>)!, i => i));

    private static readonly MethodInfo _enumerableNonEmptyAnyMethod =
        GetGenericMethod(_ => Enumerable.Any(default(IEnumerable<int>)!, default!));

    private static readonly MethodInfo _enumerableEmptyAnyMethod =
        GetGenericMethod(_ => Enumerable.Any(default(IEnumerable<int>)!));

    private static readonly MethodInfo _queryableTakeMethod =
        GetGenericMethod(_ => Queryable.Take(default(IQueryable<int>)!, 0));

    private static readonly MethodInfo _queryableSkipMethod =
       GetGenericMethod(_ => Queryable.Skip(default(IQueryable<int>)!, 0));

    public static MethodInfo QueryableSkipMethod =>
        _queryableSkipMethod;
    public static MethodInfo QueryableTakeMethod =>
        _queryableTakeMethod;
    public static MethodInfo QueryableWhereMethod =>
        _queryableWhereMethod;
    public static MethodInfo QueryableSelectMethod =>
        _queryableSelectMethod;
    public static MethodInfo EnumerableSelectMethod =>
        _enumerableSelectMethod;
    public static MethodInfo EnumerableEmptyAnyMethod =>
        _enumerableEmptyAnyMethod;
    public static MethodInfo EnumerableNonEmptyAnyMethod =>
        _enumerableNonEmptyAnyMethod;


    private static MethodInfo GetGenericMethod<TReturn>(Expression<Func<object, TReturn>> expression) =>
        GetGenericMethod(expression as Expression);

    private static MethodInfo GetGenericMethod(Expression expression)
    {
        var lamdaExpression = expression as LambdaExpression;

        Debug.Assert(expression.NodeType == ExpressionType.Lambda);
        Debug.Assert(lamdaExpression is not null);
        Debug.Assert(lamdaExpression.Body.NodeType == ExpressionType.Call);

        return ((MethodCallExpression)lamdaExpression.Body).Method.GetGenericMethodDefinition();
    }
}
