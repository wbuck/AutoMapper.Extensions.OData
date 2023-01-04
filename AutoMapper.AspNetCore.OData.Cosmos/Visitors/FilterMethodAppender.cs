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

    internal sealed class MemberAccessReplacer : ExpressionVisitor
    {
        private readonly Type currentType;
        private readonly MemberExpression replacement;

        private MemberAccessReplacer(Type currentType, MemberExpression replacement)
        {
            this.currentType = currentType;
            this.replacement = replacement;
        }

        public static Expression Replace(Type currentType, MemberExpression replacement, Expression expression) =>
            new MemberAccessReplacer(currentType, replacement).Visit(expression);

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.NodeType == ExpressionType.MemberAccess 
                && this.currentType == node.Expression.Type)
            {                
                var expr = Expression.MakeMemberAccess(this.replacement, node.Member);
                return expr;
            }
            return base.VisitMember(node);
        }
    }

    internal sealed class WhereMethodAppender : ExpressionVisitor
    {
        private readonly List<PathSegment> pathSegments;
        private readonly PathSegment collectionSegment;
        private readonly ODataQueryContext context;
        private int currentIndex;

        private WhereMethodAppender(List<PathSegment> pathSegments, ODataQueryContext context)
        {
            this.pathSegments = pathSegments;
            this.collectionSegment = this.pathSegments.First(e => e.MemberType.IsList());
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendFilters(Expression expression, List<PathSegment> pathSegments, ODataQueryContext context) =>
            new WhereMethodAppender(pathSegments, context).Visit(expression);        

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (TryGetCurrent(out var pathSegment))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = pathSegment.ParentType.GetCurrentType();

                if (nodeType == parentType && GetBinding(out var binding))
                {
                    Next();

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

                        Type segmentType = pathSegment.MemberType.GetCurrentType();

                        ParameterExpression param = Expression.Parameter(segmentType);
                        MemberExpression memberExpression = GetMemberExpression(param);

                        return assignment.Update(GetBindingExpression(binding, param, memberExpression));
                    }
                }
            }
            return base.VisitMemberInit(node);

            bool GetBinding([MaybeNullWhen(false)] out MemberAssignment binding)
            {
                binding = node.Bindings.OfType<MemberAssignment>().FirstOrDefault(b =>
                   b.Member.Name.Equals(pathSegment.MemberName));

                return binding is not null;
            }
        }

        private MemberExpression GetMemberExpression(ParameterExpression param) =>
            this.pathSegments.Skip(this.currentIndex + 1).Aggregate
            (
                Expression.MakeMemberAccess(param, this.pathSegments[this.currentIndex].Member),
                (expression, next) => Expression.MakeMemberAccess(expression, next.Member)
            );

        private Expression GetBindingExpression(MemberAssignment binding, ParameterExpression param, MemberExpression memberExpression)
        {
            var parameters = new Dictionary<string, ParameterExpression>();

            Type memberType = this.pathSegments.Last().MemberType.GetCurrentType();
            FilterHelper helper = new(parameters, this.context);

            LambdaExpression lambdaExpression = helper
                .GetFilterPart(GetFilter().Expression)
                .GetFilter(memberType, parameters, helper.LiteralName);

            lambdaExpression = Expression.Lambda
            (
                MemberAccessReplacer.Replace(memberType, memberExpression, lambdaExpression.Body),
                false,
                param
            );

            return FilterInserter.Insert(param.Type, lambdaExpression, binding);
        }

        private void Next()
        {
            if (this.currentIndex < this.pathSegments.Count)
                ++this.currentIndex;

        }

        private bool TryGetCurrent(out PathSegment pathSegment)
        {
            (var result, pathSegment) = this.currentIndex < this.pathSegments.Count
                ? (true, this.pathSegments[this.currentIndex])
                : (false, default);

            return result;
        }

        private FilterClause GetFilter() =>
            this.pathSegments.Last().FilterOptions.FilterClause;
    }

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
        private readonly List<PathSegment> filterPath;
        private readonly ODataQueryContext context;
        private int currentIndex;

        private FilterMethodAppender(List<PathSegment> filterPath, ODataQueryContext context)
        {
            this.filterPath = filterPath;
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendFilters(Expression expression, List<PathSegment> filterPath, ODataQueryContext context) =>
            new FilterMethodAppender(filterPath, context).Visit(expression);        

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {            
            if (TryGetCurrent(out var pathSegment))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = pathSegment.ParentType.GetCurrentType();            

                if (nodeType == parentType && GetBinding(out var binding))
                {                                   
                    if (pathSegment.FilterOptions?.FilterClause is not FilterClause clause)
                    {
                        Next();
                        return base.VisitMemberInit(node);
                    }

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

                        Next();
                        return assignment.Expression switch
                        {
                            MethodCallExpression expr => assignment.Update(GetCallExpression(expr, pathSegment)),
                            _ => assignment.Update(GetBindingExpression(assignment, clause))
                        };
                    }
                }
            }                              
            return base.VisitMemberInit(node);

            bool GetBinding([MaybeNullWhen(false)] out MemberAssignment binding)
            {
                 binding = node.Bindings.OfType<MemberAssignment>().FirstOrDefault(b => 
                    b.Member.Name.Equals(pathSegment.MemberName));

                return binding is not null;
            }
        }

        private static bool ListTypesAreEquivalent(Type bindingType, Type expansionType)
        {
            if (!bindingType.IsList() || !expansionType.IsList())
                return false;

            return bindingType.GetUnderlyingElementType() == expansionType.GetUnderlyingElementType();
        }

        private Expression GetCallExpression(MethodCallExpression callExpression, PathSegment pathSegment) =>
            FilterAppender.AppendFilter(callExpression, ToExpansion(pathSegment), this.context);

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
            if (this.currentIndex < this.filterPath.Count)
                ++this.currentIndex;

        }

        private bool TryGetCurrent(out PathSegment options)
        {
            (var result, options) = currentIndex < this.filterPath.Count
                ? (true, this.filterPath[this.currentIndex])
                : (false, default);

            return result;
        }

        private ODataExpansionOptions ToExpansion(in PathSegment pathSegment) =>
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
