using System;
using System.Linq;
using System.Linq.Expressions;

namespace Guts.Business.Tests.Extensions
{
    public static class ExpressionExtensions
    {
        public static bool BodyContains<T>(this Expression<T> expression, string text)
        {
            return expression.Body.ToString().ToLower().Contains(text.ToLower());
        }

        public static bool BodyContains<T>(this Expression<T> expression, params string[] texts)
        {
            return texts.All(text => BodyContains(expression, (string) text));
        }

        public static bool IsTrueFor<T>(this Expression<Func<T, bool>> filterExpression, T target)
        {
            return filterExpression.Compile().Invoke(target);
        }
    }
}