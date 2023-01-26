using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors
{
    internal abstract class VisitorBase : ExpressionVisitor
    {
        private int currentIndex;
        protected readonly List<PathSegment> pathSegments;
        protected readonly ODataQueryContext context;

        protected VisitorBase(List<PathSegment> pathSegments, ODataQueryContext context)
        {
            this.currentIndex = 0;
            this.pathSegments = pathSegments;
            this.context = context;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (TryGetCurrent(out var pathSegment))
            {
                Type nodeType = node.Type.GetCurrentType();
                Type parentType = pathSegment.ParentType.GetCurrentType();

                if (nodeType == parentType && GetMatchingBinding(node, pathSegment, out var binding))
                {
                    Next();
                    return MatchedExpression(pathSegment, node, binding);                    
                }
            }
            return base.VisitMemberInit(node);            
        }

        protected abstract Expression MatchedExpression(PathSegment pathSegment, MemberInitExpression node, MemberAssignment binding);

        protected virtual bool GetMatchingBinding(MemberInitExpression node, PathSegment pathSegment, [MaybeNullWhen(false)] out MemberAssignment binding)
        {
            binding = node.Bindings.OfType<MemberAssignment>().FirstOrDefault(b =>
               b.Member.Name.Equals(pathSegment.MemberName));

            return binding is not null;
        }

        protected int CurrentIndex() => 
            this.currentIndex;

        private void Next()
        {
            if (this.currentIndex < this.pathSegments.Count)
                ++this.currentIndex;
        }

        private bool TryGetCurrent(out PathSegment options)
        {
            (var result, options) = currentIndex < this.pathSegments.Count
                ? (true, this.pathSegments[this.currentIndex])
                : (false, default);

            return result;
        }
    }
}
