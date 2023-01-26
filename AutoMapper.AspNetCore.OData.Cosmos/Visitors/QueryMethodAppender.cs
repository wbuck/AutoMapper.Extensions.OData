using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
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
                ? GetCallExpression(lastSegment)
                : GetMemberAccessExpression(lastSegment);

            return expression;

            Expression GetCallExpression(in PathSegment segment) =>
                TopAndSkipInserter.UpdateExpression
                (
                    binding.Expression,
                    elementType,
                    segment.QueryOptions,
                    this.context
                );

            Expression GetMemberAccessExpression(in PathSegment segment)
            {
                Expression expression = binding.Expression.GetQueryableMethod
                (
                    this.context,
                    segment.QueryOptions.OrderByClause,
                    elementType,
                    segment.QueryOptions.Skip,
                    segment.QueryOptions.Top
                );

                return expression.Type.IsArray
                    ? expression.ToArrayCall(elementType)
                    : expression.ToListCall(elementType);
            }
                
        }
    }
}
