using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class MemberCollectionFilterAppender : VisitorBase
    {
        private MemberCollectionFilterAppender(List<PathSegment> pathSegments, ODataQueryContext context) 
            : base(pathSegments, context)
        {}

        public static Expression AppendFilters(Expression expression, List<PathSegment> filterPath, ODataQueryContext context) =>
            new MemberCollectionFilterAppender(filterPath, context).Visit(expression);        

        protected override Expression MatchedExpression(PathSegment pathSegment, MemberInitExpression node, MemberAssignment binding)
        {
            if (pathSegment.FilterOptions?.FilterClause is not FilterClause clause)
                return base.VisitMemberInit(node);

            if (!binding.Member.GetMemberType().IsList())
            {
                throw new NotSupportedException();
            }

            return Expression.MemberInit
            (
                Expression.New(node.Type),
                node.Bindings.OfType<MemberAssignment>().Select(UpdateBinding)
            );

            MemberAssignment UpdateBinding(MemberAssignment assignment)
            {
                if (assignment != binding)
                    return assignment;

                return assignment.Expression switch
                {
                    MethodCallExpression expr => assignment.Update(GetCallExpression(expr, pathSegment)),
                    _ => assignment.Update(GetBindingExpression(assignment, clause))
                };
            }
        }

        private Expression GetCallExpression(MethodCallExpression callExpression, in PathSegment pathSegment) =>
            FilterAppender.AppendFilter(callExpression, ToExpansion(pathSegment), this.context);

        private Expression GetBindingExpression(MemberAssignment memberAssignment, FilterClause clause)
        {
            Type memberType = memberAssignment.Member.GetMemberType();
            Type elementType = memberType.GetCurrentType();

            MethodCallExpression callExpression = Expression.Call
            (
                LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
                memberAssignment.Expression,
                clause.GenerateFilterExpression(elementType, this.context)
            );

            return memberType.IsArray
                ? callExpression.ToArrayCall(elementType)
                : callExpression.ToListCall(elementType);
        }

        private static ODataExpansionOptions ToExpansion(in PathSegment pathSegment) =>
            new()
            {
                MemberName = pathSegment.MemberName,
                MemberType = pathSegment.MemberType,
                ParentType = pathSegment.ParentType,
                FilterOptions = pathSegment.FilterOptions,
                QueryOptions = pathSegment.QueryOptions
            };        
    }
}
