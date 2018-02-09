using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data
{
    public static class DbSetExtensions
    {
        public static void AddIfNotExists<T>(this DbSet<T> dbSet, Expression<Func<T, object>> identifierExpression, T entity) where T : class
        {
            object identifierValue = identifierExpression.Compile().Invoke(entity);
            var equalsExpression = Expression.Equal(identifierExpression.Body, Expression.Constant(identifierValue));
            var filter = Expression.Lambda<Func<T, bool>>(equalsExpression, identifierExpression.Parameters.First());

            if (!dbSet.Any(filter))
            {
                dbSet.Add(entity);
            }
        }
    }
}