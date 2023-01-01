using LogicBuilder.Expressions.Utils;
using LogicBuilder.Expressions.Utils.DataSource;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class Walker : ExpressionVisitor
    {
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return base.VisitLambda<T>(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return base.VisitMethodCall(node);
        }
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return base.VisitMemberInit(node);
        }
    }

    internal sealed class NestedFilterAppender : ExpressionVisitor
    {
        private readonly List<ODataExpansionOptions> expansions;
        private readonly ODataQueryContext context;
        private int currentIndex;

        public NestedFilterAppender(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansions = expansions;
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendFilter(Expression expression, List<ODataExpansionOptions> expansions, ODataQueryContext context) =>
            new NestedFilterAppender(expansions, context).Visit(expression);

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return base.VisitLambda<T>(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {            
            if (TryGetCurrent(out var expansion))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = expansion.ParentType.GetCurrentType();            

                if (nodeType == parentType && GetBinding(out var binding))
                {
                    Advance();                 

                    if (expansion.FilterOptions?.FilterClause is null)                    
                        return base.VisitMemberInit(node);

                    List<MemberAssignment> memberAssignments = node.Bindings.OfType<MemberAssignment>().Select(expr => 
                    {
                        if (expr == binding)
                        {
                            Type elementType = expr.Expression.Type.GetCurrentType();
                            return expr.Update
                            (
                                Expression.Call
                                (
                                    LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
                                    binding.Expression,
                                    expansion.FilterOptions.FilterClause.GetFilterExpression(elementType, this.context)
                                ).ToListCall(elementType)
                            );
                        }
                        return expr;
                    }).ToList();

                    return Expression.MemberInit
                    (
                        Expression.New(node.Type),
                        memberAssignments
                    );
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
                Type elementType = node.Type.GetCurrentType();
                Type memberType = expansion.MemberType.GetCurrentType();

                if (node.Method.Name.Equals(nameof(Enumerable.Select)) 
                    && memberType == elementType)
                {
                    Advance();

                    if (expansion.FilterOptions?.FilterClause is not null)                    
                        return FilterAppender.AppendFilter(node, expansion, this.context);                    
                }
            }
            
            return base.VisitMethodCall(node);
        }

        private bool IsComplete() => 
            this.currentIndex >= this.expansions.Count;

        private void Advance() =>
            ++this.currentIndex;

        private bool TryGetCurrent([MaybeNullWhen(false)] out ODataExpansionOptions options)
        {
            options = currentIndex < this.expansions.Count
                ? this.expansions[this.currentIndex]
                : null;

            return options is not null;
        }
    }

    internal abstract class TestVisitor : ExpressionVisitor
    {
        private readonly ODataQueryContext context;
        private readonly List<Expression> foundExpansions;
        protected readonly List<ODataExpansionOptions> expansions;

        public TestVisitor(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansions = expansions;
            this.context = context;
            this.foundExpansions = new();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (TryGetCurrent(out var expansion))
            {
                Type elementType = node.Type.GetCurrentType();
                Type memberType = expansion.MemberType.GetCurrentType();

                if (node.Method.Name.Equals(nameof(Enumerable.Select))
                    && memberType == elementType)
                {
                    return GetBindingExpression(node, expansion);
                }
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (TryGetCurrent(out var expansion))
            {
                if (node.NewExpression.Type != expansion.ParentType)
                    return base.VisitMemberInit(node);

                return Expression.MemberInit
                (
                    Expression.New(node.Type),
                    node.Bindings.OfType<MemberAssignment>().Aggregate
                    (
                        new List<MemberBinding>(),
                        AddBinding
                    )
                );
            }

            return base.VisitMemberInit(node);

            List<MemberBinding> AddBinding(List<MemberBinding> list, MemberAssignment binding)
            {
                if (ListTypesAreEquivalent(binding.Member.GetMemberType(), expansion.MemberType)
                        && string.Compare(binding.Member.Name, expansion.MemberName, true) == 0) //found the expansion
                {
                    if (this.foundExpansions.Count > 0)
                        throw new NotSupportedException("Recursive queries not supported");

                    AddBindingExpression(GetBindingExpression(binding.Expression, expansion));
                }
                else
                {
                    list.Add(binding);
                }

                return list;

                void AddBindingExpression(Expression bindingExpression)
                {
                    list.Add(Expression.Bind(binding.Member, bindingExpression));
                    foundExpansions.Add(bindingExpression);
                }
            }
        }

        protected abstract Expression GetBindingExpression(Expression binding, ODataExpansionOptions expansion);

        protected abstract Expression GetCallExpression(MethodCallExpression call, ODataExpansionOptions expansion);

        protected static bool ListTypesAreEquivalent(Type bindingType, Type expansionType)
        {
            if (!bindingType.IsList() || !expansionType.IsList())
                return false;

            return bindingType.GetUnderlyingElementType() == expansionType.GetUnderlyingElementType();
        }

        private bool TryGetCurrent([MaybeNullWhen(false)] out ODataExpansionOptions options)
        {
            options = this.expansions.FirstOrDefault();
            return options is not null;
        }
    }

    internal abstract class ProjectionVisitor : ExpressionVisitor
    {
        public ProjectionVisitor(List<ODataExpansionOptions> expansions)
        {
            this.expansions = expansions;
        }

        protected readonly List<ODataExpansionOptions> expansions;
        private readonly List<Expression> foundExpansions = new();

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            ODataExpansionOptions expansion = expansions.First();

            if (node.NewExpression.Type != expansion.ParentType)
                return base.VisitMemberInit(node);

            return Expression.MemberInit
            (
                Expression.New(node.Type),
                node.Bindings.OfType<MemberAssignment>().Aggregate
                (
                    new List<MemberBinding>(),
                    AddBinding
                )
            );

            List<MemberBinding> AddBinding(List<MemberBinding> list, MemberAssignment binding)
            {
                if (ListTypesAreEquivalent(binding.Member.GetMemberType(), expansion.MemberType)
                        && string.Compare(binding.Member.Name, expansion.MemberName, true) == 0)//found the expansion
                {
                    if (foundExpansions.Count > 0)
                        throw new NotSupportedException("Recursive queries not supported");

                    AddBindingExpression(GetBindingExpression(binding, expansion));
                }
                else
                {
                    list.Add(binding);
                }

                return list;

                void AddBindingExpression(Expression bindingExpression)
                {
                    list.Add(Expression.Bind(binding.Member, bindingExpression));
                    foundExpansions.Add(bindingExpression);
                }
            }
        }

        protected abstract Expression GetBindingExpression(MemberAssignment binding, ODataExpansionOptions expansion);

        protected static bool ListTypesAreEquivalent(Type bindingType, Type expansionType)
        {
            if (!bindingType.IsList() || !expansionType.IsList())
                return false;

            return bindingType.GetUnderlyingElementType() == expansionType.GetUnderlyingElementType();
        }
    }
}
