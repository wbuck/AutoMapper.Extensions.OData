using LogicBuilder.Expressions.Utils;
using LogicBuilder.Expressions.Utils.ExpressionBuilder;
using LogicBuilder.Expressions.Utils.ExpressionBuilder.Operand;
using System;
using System.Linq.Expressions;

namespace AutoMapper.AspNet.OData.Operators
{
    internal sealed class ConvertEnumToUnderlyingOperator : IExpressionPart
    {
        private readonly IExpressionPart expressionPart;

        public ConvertEnumToUnderlyingOperator(IExpressionPart expressionPart)
        {
            this.expressionPart = expressionPart;
        }

        public Expression Build() =>
            Build(expressionPart.Build());

        private Expression GetConstantExpression(ConstantExpression constant, Type underlyingEnumType)
        {
            var property = constant.Value.GetType()
                .GetProperty(nameof(ConstantContainer<object>.TypedProperty));

            if (property is not null)
            {
                object value = property.GetValue(constant.Value);
                return GetConstant(value, underlyingEnumType);
            }

            return GetConstant(constant.Value, underlyingEnumType);

            static Expression GetConstant(object value, Type type) =>
                Expression.Constant
                (
                    Convert.ChangeType(value, type),
                    type
                );
        }

        private Expression Build(Expression expression)
        {
            Type underlyingType = expression.Type.ToNullableUnderlyingType();

            if (!underlyingType.IsEnum)
                return expression;

            Type underlyingEnumType = underlyingType.GetEnumUnderlyingType();

            if (expression is ParameterExpression)
            {
                return GetConvertExpression(expression, underlyingEnumType);
            }
            else if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is ConstantExpression constant)
                    return GetConstantExpression(constant, underlyingEnumType);
                else
                    return GetConvertExpression(expression, underlyingEnumType);
            }

            return expression;
        }

        private Expression GetConvertExpression(Expression expression, Type underlyingEnumType)
        {
            Type conversionType = expression.Type.IsNullableType()
                ? underlyingEnumType.ToNullable()
                : underlyingEnumType;

            return Expression.Convert(expression, conversionType);
        }
    }
}
