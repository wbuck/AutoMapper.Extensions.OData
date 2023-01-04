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
    internal sealed class WhereAppender : ExpressionVisitor
    {
        private readonly Type type;
        private readonly LambdaExpression lambdaExpression;        

        public WhereAppender(Type type, LambdaExpression lambdaExpression)
        {
            this.type = type;
            this.lambdaExpression = lambdaExpression;            
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Type elementType = node.Type.GetCurrentType();
            if (node.Method.Name.Equals(nameof(Enumerable.Select)) && elementType == this.type)
            {
                var expr = Expression.Call
                (
                    LinqMethods.EnumerableWhereMethod.MakeGenericMethod(type),
                    node,
                    this.lambdaExpression
                );
                return expr;
            }
            return base.VisitMethodCall(node);
        }
    }

    internal sealed class MemberAccessReplacer : ExpressionVisitor
    {
        private readonly Type currentType;
        private readonly MemberExpression replacement;

        public MemberAccessReplacer(Type currentType, MemberExpression replacement)
        {
            this.currentType = currentType;
            this.replacement = replacement;
        }
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
        private readonly List<PathSegment> filterPath;
        private readonly PathSegment collectionSegment;
        private readonly ODataQueryContext context;
        private int currentIndex;

        private WhereMethodAppender(List<PathSegment> filterPath, ODataQueryContext context)
        {
            this.filterPath = filterPath;
            this.collectionSegment = this.filterPath.First(e => e.MemberType.IsList());
            this.context = context;
            this.currentIndex = 0;
        }

        public static Expression AppendFilters(Expression expression, List<PathSegment> filterPath, ODataQueryContext context) =>
            new WhereMethodAppender(filterPath, context).Visit(expression);

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (TryGetCurrent(out var pathSegemt))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = pathSegemt.ParentType.GetCurrentType();

                if (nodeType == parentType && GetBinding(out var binding))
                {
                    if (pathSegemt != this.collectionSegment)
                    {
                        Next();
                        return base.VisitMemberInit(node);
                    }

                    // Where(Select, $it.Dc.FullyQualifiedDomainName == "Some Value"))
                    var lastExpansion = this.filterPath.Last();
                    FilterClause clause = lastExpansion.FilterOptions.FilterClause;

                    ParameterExpression param = Expression.Parameter(pathSegemt.MemberType.GetCurrentType());

                    var access = Expression.MakeMemberAccess(param, pathSegemt.MemberType.GetCurrentType().GetPropertiesAndFields().First(m => m.Name.Equals(lastExpansion.MemberName)));
                    var parameters = new Dictionary<string, ParameterExpression>();

                    Type memberType = lastExpansion.MemberType.GetCurrentType();
                    FilterHelper helper = new(parameters, this.context);

                    
                    var lambda = helper.GetFilterPart(clause.Expression).GetFilter(memberType, parameters, helper.LiteralName);
                    
                    //lambda = (LambdaExpression)new LambdaReplacer(lambda.Parameters.First(), param, access).Visit(lambda);

                    var lambdaTest = Expression.Lambda
                    (
                        new MemberAccessReplacer(memberType, access).Visit(lambda.Body),
                        false,
                        param
                    );

                    var bindingExpression = new WhereAppender(param.Type, lambdaTest).Visit(binding.Expression);
                   
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
                        var expr = assignment.Update(bindingExpression);
                        return expr;
                        //return assignment.Expression switch
                        //{
                        //    MethodCallExpression expr => assignment.Update(GetCallExpression(expr, expansion)),
                        //    _ => assignment.Update(GetBindingExpression(assignment, clause))
                        //};
                    }
                }
            }
            return base.VisitMemberInit(node);

            bool GetBinding([MaybeNullWhen(false)] out MemberAssignment binding)
            {
                binding = node.Bindings.OfType<MemberAssignment>().FirstOrDefault(b =>
                   b.Member.Name.Equals(pathSegemt.MemberName));

                return binding is not null;
            }
        }

        //private Expression GetCallExpression(MethodCallExpression callExpression, ODataExpansionOptions expansion) =>
        //    FilterAppender.AppendFilter(callExpression, expansion, this.context);
        //
        //private Expression GetBindingExpression(MemberAssignment memberAssignment, FilterClause clause)
        //{
        //    Type elementType = memberAssignment.Expression.Type.GetCurrentType();
        //    return Expression.Call
        //    (
        //        LinqMethods.EnumerableWhereMethod.MakeGenericMethod(elementType),
        //        memberAssignment.Expression,
        //        clause.GetFilterExpression(elementType, context)
        //    ).ToListCall(elementType);
        //}

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
