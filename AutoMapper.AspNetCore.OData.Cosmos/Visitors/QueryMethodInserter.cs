#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class QueryMethodInserter : ExpressionVisitor
    {
        private readonly IReadOnlyList<PathSegment> pathSegments;
        private readonly QueryOptions options;
        private readonly ODataQueryContext context;
        private readonly Type elementType;

        private QueryMethodInserter(IReadOnlyList<PathSegment> pathSegments, QueryOptions options, ODataQueryContext context)
        {
            this.pathSegments = pathSegments;
            this.options = options;
            this.context = context;
            this.elementType = pathSegments[0].ElementType;
        }

        public static Expression Insert(IReadOnlyList<PathSegment> pathSegments, QueryOptions options, ODataQueryContext context, Expression expression) =>
            new QueryMethodInserter(pathSegments, options, context).Visit(expression);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Type elementType = node.Type.GetCurrentType();
            if (node.Method.Name.Equals(nameof(Enumerable.Select)) && elementType == this.elementType)
            {
                if (this.options.OrderByClause is null && (this.options.Skip is not null || this.options.Top is not null))
                {
                    OrderBySetting? settings = context.FindSortableProperties(elementType);

                    Expression expression = settings is not null
                        ? GetDefaultOrderByCall(node, settings)
                        : node;

                    expression = expression
                        .GetSkipCall(this.options.Skip)
                        .GetTakeCall(this.options.Top);

                    return expression;
                }
                if (this.options.OrderByClause is not null)
                {
                    Expression expression = GetOrderByExpression(node, this.options.OrderByClause)
                        .GetSkipCall(this.options.Skip)
                        .GetTakeCall(this.options.Top);

                    return expression;
                }                                
            }
            return base.VisitMethodCall(node);
        }

        private Expression GetDefaultThenByCall(Expression expression, OrderBySetting settings)
        {
            return settings.ThenBy is null
                ? GetMethodCall()
                : GetDefaultThenByCall(GetMethodCall(), settings.ThenBy);

            Expression GetMethodCall() =>
                expression.GetOrderByCall(settings.Name, nameof(Enumerable.OrderBy));
        }

        private Expression GetDefaultOrderByCall(Expression expression, OrderBySetting settings)
        {
            return settings.ThenBy is null
                ? GetMethodCall()
                : GetDefaultThenByCall(GetMethodCall(), settings.ThenBy);

            Expression GetMethodCall() =>
                expression.GetOrderByCall(settings.Name, nameof(Queryable.OrderBy));
        }

        private Expression GetThenByExpression(Expression expression, OrderByClause clause)
        {
            const string ThenBy = nameof(Enumerable.ThenByDescending);
            const string ThenByDescending = nameof(Enumerable.ThenByDescending);

            string method = clause.Direction == OrderByDirection.Descending
                ? ThenByDescending
                : ThenBy;

            return clause.ThenBy is null
                ? GetCall()
                : GetThenByExpression(GetCall(), clause.ThenBy);

            Expression GetCall() => clause.Expression switch
            {
                CountNode node => throw new NotImplementedException(),
                _ => GetCallExpression(expression, clause.GetSelector(this.pathSegments), method)
            };
        }

        private Expression GetOrderByExpression(Expression expression, OrderByClause clause)
        {
            const string OrderBy = nameof(Enumerable.OrderBy);
            const string OrderByDescending = nameof(Enumerable.OrderByDescending);

            string method = clause.Direction == OrderByDirection.Descending
                ? OrderByDescending
                : OrderBy;

            return clause.ThenBy is null
                ? GetCall()
                : GetThenByExpression(GetCall(), clause.ThenBy);

            Expression GetCall() => clause.Expression switch
            {
                CountNode node => throw new NotImplementedException(),
                _ => GetCallExpression(expression, clause.GetSelector(this.pathSegments), method)
            };
        }

        private MethodCallExpression GetCallExpression(Expression expression, LambdaExpression selector, string methodName)
        {
            Type collectionType = expression.Type.IsIQueryable() 
                ? typeof(Queryable) 
                : typeof(Enumerable);

            return Expression.Call
            (
                collectionType,
                methodName,
                new[] { this.elementType, selector.Body.Type },
                expression,
                selector
            );
        }

    }
}
