using AutoMapper.Execution;
using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class QueryMethodAppender : ExpressionVisitor
    {
        private readonly List<ODataExpansionOptions> expansionPath;
        private readonly ODataQueryContext context;
        private int currentIndex;

        public QueryMethodAppender(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansionPath = expansions;
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendQuery(Expression expression, List<ODataExpansionOptions> expansions, ODataQueryContext context) =>
            new QueryMethodAppender(expansions, context).Visit(expression);

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (TryGetCurrent(out var expansion))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = expansion.ParentType.GetCurrentType();

                if (nodeType == parentType && GetBinding(out var binding))
                {
                    if (expansion.QueryOptions is not QueryOptions options)
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

                        Next();
                        return assignment.Expression switch
                        {
                            MethodCallExpression expr => assignment.Update(GetCallExpression(expr, expansion)),
                            _ => assignment.Update(GetBindingExpression(assignment, options))
                        };
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

        private Expression GetCallExpression(MethodCallExpression callExpression, ODataExpansionOptions expansion) =>
            MethodAppender.AppendQueryMethod(callExpression, expansion, this.context);

        private Expression GetBindingExpression(MemberAssignment memberAssignment, QueryOptions options)
        {
            return memberAssignment.Expression;
            //Type elementType = memberAssignment.Expression.Type.GetCurrentType();
            //return Expression.Call
            //(
            //    LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
            //    memberAssignment.Expression,
            //    clause.GetFilterExpression(elementType, context)
            //).ToListCall(elementType);
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

                    if (!binding.Member.GetMemberType().IsList())
                    {
                        
                        Debugger.Break();
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

                        Next();
                        return assignment.Expression switch
                        {
                            MethodCallExpression expr => assignment.Update(GetCallExpression(expr, expansion)),
                            _ => assignment.Update(GetBindingExpression(assignment, clause))
                        };
                    }
                }
            }                              
            return base.VisitMemberInit(node);

            bool GetBinding([MaybeNullWhen(false)] out MemberAssignment binding)
            {
                 binding = node.Bindings.OfType<MemberAssignment>().FirstOrDefault(b => 
                    b.Member.Name.Equals(expansion.MemberName));

                return binding is not null;
            }
        }

        private static bool ListTypesAreEquivalent(Type bindingType, Type expansionType)
        {
            if (!bindingType.IsList() || !expansionType.IsList())
                return false;

            return bindingType.GetUnderlyingElementType() == expansionType.GetUnderlyingElementType();
        }

        private Expression GetCallExpression(MethodCallExpression callExpression, ODataExpansionOptions expansion) =>
            FilterAppender.AppendFilter(callExpression, expansion, this.context);

        private Expression GetBindingExpression(MemberAssignment memberAssignment, FilterClause clause)
        {
            Type elementType = memberAssignment.Expression.Type.GetCurrentType();
            return Expression.Call
            (
                LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
                memberAssignment.Expression,
                clause.GetFilterExpression(elementType, context)
            ).ToListCall(elementType);
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
