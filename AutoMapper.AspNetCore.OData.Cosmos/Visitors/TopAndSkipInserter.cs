using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class TopAndSkipInserter : ExpressionVisitor
    {
        private readonly Type elementType;
        private readonly QueryOptions options;
        private readonly ODataQueryContext context;

        private TopAndSkipInserter(Type elementType, QueryOptions options, ODataQueryContext context)
        {
            this.elementType = elementType;
            this.options = options;
            this.context = context;
        }

        public static Expression UpdateExpression(Expression expression, Type elementType, QueryOptions options, ODataQueryContext context) =>
            new TopAndSkipInserter(elementType, options, context).Visit(expression);        

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Type elementType = node.Type.GetCurrentType();
            if (node.Method.Name.Equals(nameof(Enumerable.Select)) && elementType == this.elementType)
            {
                //Expression collection = node.Arguments[0];
                Expression expression = node.GetQueryableMethod
                (
                    this.context,
                    this.options.OrderByClause,
                    elementType,
                    this.options.Skip,
                    this.options.Top
                );

                ParameterExpression parameter = Expression.Parameter
                (
                    expression.Type.GetUnderlyingElementType(), 
                    "it"
                );
                expression = DollarSignParameterReplacer.Replace(expression, parameter);
                return expression;

                //return node.Update
                //(
                //    node.Object,
                //    new[] { expression, node.Arguments[1] }
                //);
            }
            return base.VisitMethodCall(node);
        }
    }
}
