using System.Linq.Expressions;

namespace JobScraper.Utils.Extensions;

public static class EfLinqExtensions
{
    public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> queryable,
        bool condition, Expression<Func<TSource, bool>> predicate)
    {
        if (!condition)
            return queryable;

        return queryable.Where(predicate);
    }
}