using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class FilterMethodAppender : ExpressionVisitor
    {
        private readonly List<ODataExpansionOptions> expansionPath;
        private readonly ODataQueryContext context;
        private int currentIndex;

        public FilterMethodAppender(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansionPath = expansions;
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
                    if (expansion.FilterOptions?.FilterClause is not FilterClause clause)
                    {
                        Next();
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
                        {
                            Next();
                            return assignment.Update(FilterAppender.AppendFilter(callExpression, expansion, this.context));
                        }

                        Next();
                        Type elementType = assignment.Expression.Type.GetCurrentType();

                        return assignment.Update
                        (
                            Expression.Call
                            (
                                LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
                                binding.Expression,
                                clause.GetFilterExpression(elementType, this.context)
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

        private void Next() 
        {
            if (this.currentIndex < this.expansionPath.Count)
                ++this.currentIndex;

        }

        private bool TryGetCurrent([MaybeNullWhen(false)] out ODataExpansionOptions options)
        {
            options = currentIndex < this.expansionPath.Count
                ? this.expansionPath[this.currentIndex]
                : null;

            return options is not null;
        }
    }
}
