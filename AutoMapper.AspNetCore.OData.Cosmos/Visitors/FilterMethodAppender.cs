using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class FilterMethodAppender : ExpressionVisitor
    {
        private readonly List<ODataExpansionOptions> expansions;
        private readonly ODataQueryContext context;
        private int currentIndex;

        public FilterMethodAppender(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansions = expansions;
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendFilters(Expression expression, List<ODataExpansionOptions> expansions, ODataQueryContext context) =>
            new FilterMethodAppender(expansions, context).Visit(expression);

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {            
            if (TryGetCurrent(out var expansion))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = expansion.ParentType.GetCurrentType();            

                if (nodeType == parentType && GetBinding(out var binding))
                {                                   
                    if (expansion.FilterOptions?.FilterClause is null)
                    {
                        Advance();
                        return base.VisitMemberInit(node);
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

                        if (assignment.Expression is MethodCallExpression callExpression)
                            return assignment.Update(Visit(callExpression));

                        Advance();
                        Type elementType = assignment.Expression.Type.GetCurrentType();

                        return assignment.Update
                        (
                            Expression.Call
                            (
                                LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
                                binding.Expression,
                                expansion.FilterOptions.FilterClause.GetFilterExpression(elementType, this.context)
                            ).ToListCall(elementType)
                        );

                    }
                }
            }                              
            return base.VisitMemberInit(node);

            bool GetBinding([MaybeNullWhen(false)] out MemberAssignment binding)
            {
                 binding = node.Bindings.OfType<MemberAssignment>()
                    .FirstOrDefault(b => b.Member.Name.Equals(expansion.MemberName));

                return binding is not null;
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (TryGetCurrent(out var expansion))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type memberType = expansion.MemberType.GetCurrentType();

                if (node.Method.Name.Equals(nameof(Enumerable.Select)) 
                    && memberType == nodeType)
                {
                    Advance();

                    if (expansion.FilterOptions?.FilterClause is not null)                    
                        return FilterAppender.AppendFilter(node, expansion, this.context);                    
                }
            }
            
            return base.VisitMethodCall(node);
        }

        private void Advance() 
        {
            if (this.currentIndex < this.expansions.Count)
                ++this.currentIndex;

        }

        private bool TryGetCurrent([MaybeNullWhen(false)] out ODataExpansionOptions options)
        {
            options = currentIndex < this.expansions.Count
                ? this.expansions[this.currentIndex]
                : null;

            return options is not null;
        }
    }
}
