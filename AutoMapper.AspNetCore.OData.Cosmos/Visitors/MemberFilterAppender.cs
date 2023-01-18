using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class MemberFilterAppender : VisitorBase
    {
        private readonly PathSegment collectionSegment;

        private MemberFilterAppender(List<PathSegment> pathSegments, ODataQueryContext context) 
            : base(pathSegments, context)
        {
            this.collectionSegment = this.pathSegments.Last(e => e.MemberType.IsList());
        }

        public static Expression AppendFilters(Expression expression, List<PathSegment> pathSegments, ODataQueryContext context) =>
            new MemberFilterAppender(pathSegments, context).Visit(expression);

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

                Type segmentType = pathSegment.ElementType;

                ParameterExpression param = Expression.Parameter(segmentType);
                MemberExpression memberExpression = GetMemberExpression(param);

                return assignment.Update(GetBindingExpression(binding, param, memberExpression));
            }
        }

        private MemberExpression GetMemberExpression(ParameterExpression param) =>
            this.pathSegments.Skip(CurrentIndex() + 1).Aggregate
            (
                Expression.MakeMemberAccess(param, this.pathSegments[CurrentIndex()].Member),
                (expression, next) => Expression.MakeMemberAccess(expression, next.Member)
            );

        private Expression GetBindingExpression(MemberAssignment binding, ParameterExpression param, MemberExpression memberExpression)
        {
            var parameters = new Dictionary<string, ParameterExpression>();

            Type memberType = this.pathSegments.Last().ElementType;
            FilterClause clause = GetFilter();

            LambdaExpression lambdaExpression = new FilterHelper(parameters, this.context)
                .GetFilterPart(clause.Expression)
                .GetFilter(memberType, parameters, clause.RangeVariable.Name);

            lambdaExpression = Expression.Lambda
            (
                MemberAccessReplacer.Replace(memberType, memberExpression, lambdaExpression.Body),
                false,
                param
            );

            return FilterInserter.Insert(param.Type, lambdaExpression, binding);
        }

        private FilterClause GetFilter() =>
            this.pathSegments.Last().FilterOptions.FilterClause;        
    }
}
