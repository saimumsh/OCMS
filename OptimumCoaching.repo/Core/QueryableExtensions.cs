using OptimumCoaching.core.Core;
using System.Linq.Expressions;

namespace OptimumCoaching.repo.Core
{
    public static class QueryableExtensions
    {
        public static IQueryable<TEntity> ApplyOrdering<TEntity>(
            this IQueryable<TEntity> query,
            IQueryObject? queryObj,
            Dictionary<string, Expression<Func<TEntity, object>>>? columnsMap)
        {
            if (queryObj == null || columnsMap == null) return query;
            if (string.IsNullOrWhiteSpace(queryObj.SortBy) || !columnsMap.ContainsKey(queryObj.SortBy))
                return query;

            return queryObj.IsSortAscending
                ? query.OrderBy(columnsMap[queryObj.SortBy])
                : query.OrderByDescending(columnsMap[queryObj.SortBy]);
        }

        public static IQueryable<TEntity> ApplyPaging<TEntity>(
            this IQueryable<TEntity> query, IQueryObject? queryObj)
        {
            if (queryObj == null) return query;

            if (queryObj.Page <= 0) queryObj.Page = 1;
            if (queryObj.PageSize <= 0) queryObj.PageSize = 50;

            return query.Skip((queryObj.Page - 1) * queryObj.PageSize).Take(queryObj.PageSize);
        }
    }
}
