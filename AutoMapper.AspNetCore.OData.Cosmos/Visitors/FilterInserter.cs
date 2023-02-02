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
    internal sealed class FilterInserter : ExpressionVisitor
    {
        private readonly Type elementType;
        private readonly LambdaExpression lambdaExpression;        

        private FilterInserter(Type elementType, LambdaExpression lambdaExpression)
        {
            this.elementType = elementType;
            this.lambdaExpression = lambdaExpression;            
        }

        public static Expression Insert(Type elementType, LambdaExpression lambdaExpression, MemberAssignment binding) =>
            new FilterInserter(elementType, lambdaExpression).Visit(binding.Expression);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Type elementType = node.Type.GetCurrentType();
            if (node.Method.Name.Equals(nameof(Enumerable.Select)) && elementType == this.elementType)
            {
                return Expression.Call
                (
                    LinqMethods.EnumerableWhereMethod.MakeGenericMethod(this.elementType),
                    node,
                    this.lambdaExpression
                );                
            }
            return base.VisitMethodCall(node);
        }
    }
}
