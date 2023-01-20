#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper.AspNet.OData;
internal class LinqMethods
{
    private static readonly MethodInfo _queryableWhereMethod =
        GetGenericMethod(_ => Queryable.Where(default(IQueryable<int>)!, default(Expression<Func<int, bool>>)!));

    private static readonly MethodInfo _enumerableWhereMethod =
        GetGenericMethod(_ => Enumerable.Where(default(IEnumerable<bool>)!, i => i));

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

    private static readonly MethodInfo _enumerableOrderByMethod =
        GetGenericMethod(_ => Enumerable.OrderBy(default(IQueryable<int>)!, i => i));

    private static readonly MethodInfo _enumerableOrderByDescendingMethod =
        GetGenericMethod(_ => Enumerable.OrderByDescending(default(IQueryable<int>)!, i => i));

    private static readonly MethodInfo _enumerableToListMethod =
        GetGenericMethod(_ => Enumerable.ToList(default(IEnumerable<int>)!));

    private static readonly MethodInfo _enumerableToArrayMethod =
        GetGenericMethod(_ => Enumerable.ToArray(default(IEnumerable<int>)!));

    public static MethodInfo EnumerableWhereMethod =>
        _enumerableWhereMethod;
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
    public static MethodInfo EnumerableOrderByMethod =>
        _enumerableOrderByMethod;
    public static MethodInfo EnumerableOrderByDescendingMethod =>
        _enumerableOrderByDescendingMethod;
    public static MethodInfo EnumerableToListMethod =>
        _enumerableToListMethod;
    public static MethodInfo EnumerableToArrayMethod =>
        _enumerableToArrayMethod;

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
