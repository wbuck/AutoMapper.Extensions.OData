using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal sealed class CallVisitor : ExpressionVisitor
    {
        private List<ODataExpansionOptions> expansions;
        private readonly ODataQueryContext context;        

        public CallVisitor(List<ODataExpansionOptions> expansions, ODataQueryContext context)
        {
            this.expansions = expansions;
            this.context = context;
        }

        public static Expression Traverse(Expression expression, List<ODataExpansionOptions> expansions, ODataQueryContext context) =>
            new CallVisitor(expansions, context).Visit(expression);

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var expansion = this.expansions.FirstOrDefault();
            if (expansion is not null)
            {
                Type elementType = node.Type.GetCurrentType();
                Type memberType = expansion.MemberType.GetCurrentType();

                if (elementType == memberType)
                {
                    this.expansions = this.expansions.Skip(1).ToList();
                    return Visit(node);
                }
            }            
            return base.VisitMemberInit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var expansion = this.expansions.FirstOrDefault();

            if (expansion is not null)
            {
                Type elementType = node.Type.GetCurrentType();
                Type memberType = expansion.MemberType.GetCurrentType();

                if (node.Method.Name.Equals(nameof(Enumerable.Select)) && memberType == elementType)
                {
                    if (expansion.FilterOptions?.FilterClause is FilterClause clause)
                    {
                        var expr = Expression.Call
                        (
                            node.Method.DeclaringType,
                            nameof(Enumerable.Where),
                            new Type[] { elementType },
                            node,
                            clause.GetFilterExpression(elementType, this.context)
                        );
                        return expr;
                    }

                    this.expansions = this.expansions.Skip(1).ToList();
                    return Visit(node);
                }
            }
            
            return base.VisitMethodCall(node);
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
