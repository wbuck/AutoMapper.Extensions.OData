using System.Linq;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Visitors;
internal sealed class DollarSignParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression replacement;

    private DollarSignParameterReplacer(ParameterExpression replacement)
        => this.replacement = replacement;

    public static Expression Replace(Expression expression, ParameterExpression replacement) =>
        new DollarSignParameterReplacer(replacement).Visit(expression);

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node.Name is not null && node.Name.StartsWith('$') && node.Type == this.replacement.Type)        
            return this.replacement;
        
        return base.VisitParameter(node);
    }
}
