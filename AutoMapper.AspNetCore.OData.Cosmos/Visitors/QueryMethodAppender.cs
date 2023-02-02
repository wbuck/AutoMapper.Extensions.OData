using LogicBuilder.Expressions.Utils;
using LogicBuilder.Expressions.Utils.Expansions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Layouts;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class QueryMethodAppender : VisitorBase
    {
        private readonly PathSegment collectionSegment;

        private QueryMethodAppender(List<PathSegment> pathSegments, ODataQueryContext context) 
            : base(pathSegments, context)
        {
            this.collectionSegment = this.pathSegments.Last(e => e.MemberType.IsList());
        }

        public static Expression AppendFilters(Expression expression, List<PathSegment> pathSegments, ODataQueryContext context) =>
            new QueryMethodAppender(pathSegments, context).Visit(expression);

        protected override Expression MatchedExpression(PathSegment pathSegment, MemberInitExpression node, MemberAssignment binding)
        {
            if (pathSegment != this.collectionSegment)
                return base.VisitMemberInit(node);

            return Expression.MemberInit
            (
                Expression.New(node.Type),
                node.Bindings.OfType<MemberAssignment>().Select(UpdateBinding)
            );

            MemberAssignment UpdateBinding(MemberAssignment assignment)
            {
                if (assignment != binding)
                    return assignment;

                return assignment.Update(GetBindingExpression(binding));
            }
        }

        private Expression GetBindingExpression(MemberAssignment binding)
        {
            Span<PathSegment> segments = CollectionsMarshal.AsSpan(this.pathSegments);
            ref PathSegment lastSegment = ref segments[^1];

            Type elementType = this.collectionSegment.ElementType;

            Expression expression = binding.Expression.NodeType == ExpressionType.Call
                ? GetCallExpression(binding.Expression, lastSegment)
                : GetMemberAccessExpression(binding.Expression, lastSegment);            

            return expression;            

            Expression GetCallExpression(Expression expression, in PathSegment segment) =>
                expression.GetQueryableExpression
                (
                    this.pathSegments, 
                    segment.QueryOptions, 
                    this.context
                );

            Expression GetMemberAccessExpression(Expression expression, in PathSegment segment)
            {
                Expression queryExpression = expression.GetQueryableMethod
                (
                    this.context,
                    segment.QueryOptions.OrderByClause,
                    elementType,
                    segment.QueryOptions.Skip,
                    segment.QueryOptions.Top
                );

                return queryExpression.Type.IsArray
                    ? queryExpression.ToArrayCall(elementType)
                    : queryExpression.ToListCall(elementType);
            }
                
        }

        private LambdaExpression? BuildOrderByLambdaExpression(in PathSegment lastSegment)
        {
            Type elementType = this.collectionSegment.ElementType;
            ParameterExpression parameter = Expression.Parameter(elementType, "c");

            int count = this.pathSegments.IndexOf(this.collectionSegment) + 1;

            if (count == 0)
                return null;

            Expression memberExpression = this.pathSegments
                .Skip(count)
                .Aggregate((Expression)parameter, (expression, next) => Expression.MakeMemberAccess(expression, next.Member));


            var node = (SingleValuePropertyAccessNode)lastSegment.QueryOptions.OrderByClause.Expression;
            string path = node.GetPropertyPath();

            var test2 = (SingleValuePropertyAccessNode)lastSegment.QueryOptions.OrderByClause.ThenBy.Expression;
            string path2 = test2.GetPropertyPath();

            var member = lastSegment.MemberType.GetMember(path).First();
            memberExpression = Expression.MakeMemberAccess(memberExpression, member);

            var test = Expression.Lambda
            (
                memberExpression,
                parameter
            );
            
            return test;
        }
    }
}
