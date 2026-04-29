using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core.Core;
using System.Linq.Expressions;

namespace OptimumCoaching.repo.Core
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        protected readonly ApplicationDbContext DbContext;

        public Repository(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        protected IQueryable<TEntity> BaseQuery() =>
            DbContext.Set<TEntity>().Where(e => !e.IsDeleted);

        // ----- GetAll overloads -----
        public virtual async Task<QueryResult<TEntity>> GetAllAsync(
            bool includeRelated = false,
            IQueryObject? queryObj = null,
            Dictionary<string, Expression<Func<TEntity, object>>>? columnsMap = null)
        {
            var query = BaseQuery();
            var result = new QueryResult<TEntity> { TotalItems = await query.CountAsync() };

            query = query.ApplyOrdering(queryObj, columnsMap).ApplyPaging(queryObj);
            query = ApplyIncludes(query, includeRelated);

            result.Items = await query.AsNoTracking().ToListAsync();
            return result;
        }

        public virtual async Task<QueryResult<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool includeRelated = false,
            IQueryObject? queryObj = null,
            Dictionary<string, Expression<Func<TEntity, object>>>? columnsMap = null)
        {
            var query = BaseQuery().Where(predicate);
            var result = new QueryResult<TEntity> { TotalItems = await query.CountAsync() };

            query = query.ApplyOrdering(queryObj, columnsMap).ApplyPaging(queryObj);
            query = ApplyIncludes(query, includeRelated);

            result.Items = await query.AsNoTracking().ToListAsync();
            return result;
        }

        public virtual async Task<QueryResult<TEntity>> GetAllAsync(
            IQueryObject? queryObj = null,
            Dictionary<string, Expression<Func<TEntity, object>>>? columnsMap = null,
            params Expression<Func<TEntity, object>>[] properties)
        {
            var query = BaseQuery();
            var result = new QueryResult<TEntity> { TotalItems = await query.CountAsync() };

            query = query.ApplyOrdering(queryObj, columnsMap).ApplyPaging(queryObj);
            query = properties.Aggregate(query, (current, p) => current.Include(p));

            result.Items = await query.AsNoTracking().ToListAsync();
            return result;
        }

        public virtual async Task<QueryResult<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> predicate,
            IQueryObject? queryObj = null,
            Dictionary<string, Expression<Func<TEntity, object>>>? columnsMap = null,
            params Expression<Func<TEntity, object>>[] properties)
        {
            var query = BaseQuery().Where(predicate);
            var result = new QueryResult<TEntity> { TotalItems = await query.CountAsync() };

            query = query.ApplyOrdering(queryObj, columnsMap).ApplyPaging(queryObj);
            query = properties.Aggregate(query, (current, p) => current.Include(p));

            result.Items = await query.AsNoTracking().ToListAsync();
            return result;
        }

        public virtual Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate) =>
            DbContext.Set<TEntity>().Where(predicate).LongCountAsync();

        // ----- GetById overloads -----
        public virtual async Task<TEntity?> GetByIdAsync(Guid id, bool includeRelated = false)
        {
            var query = DbContext.Set<TEntity>().Where(e => e.Id == id);
            query = ApplyIncludes(query, includeRelated);
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(
            Expression<Func<TEntity, bool>> predicate, Guid id, bool includeRelated = false)
        {
            var query = DbContext.Set<TEntity>().Where(e => e.Id == id).Where(predicate);
            query = ApplyIncludes(query, includeRelated);
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] properties)
        {
            var query = DbContext.Set<TEntity>().Where(predicate);
            query = properties.Aggregate(query, (current, p) => current.Include(p));
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(
            Guid id, params Expression<Func<TEntity, object>>[] properties)
        {
            var query = DbContext.Set<TEntity>().Where(e => e.Id == id);
            query = properties.Aggregate(query, (current, p) => current.Include(p));
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public virtual async Task<TEntity?> GetSingleItemAsync(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] properties)
        {
            var query = DbContext.Set<TEntity>().Where(predicate);
            query = properties.Aggregate(query, (current, p) => current.Include(p));
            return await query.FirstOrDefaultAsync();
        }

        // ----- Mutations -----
        public virtual Task AddAsync(TEntity entity) => DbContext.Set<TEntity>().AddAsync(entity).AsTask();

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities) =>
            DbContext.Set<TEntity>().AddRangeAsync(entities);

        public virtual Task UpdateAsync(TEntity entity)
        {
            DbContext.Set<TEntity>().Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            DbContext.Set<TEntity>().UpdateRange(entities);
            return Task.CompletedTask;
        }

        public virtual async Task ActiveInactiveAsync(Guid id)
        {
            var entity = await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity != null) await ActiveInactiveAsync(entity);
        }

        public virtual async Task ActiveInactiveAsync(Expression<Func<TEntity, bool>> predicate, Guid id)
        {
            var entity = await DbContext.Set<TEntity>()
                .Where(e => e.Id == id).Where(predicate).FirstOrDefaultAsync();
            if (entity != null) await ActiveInactiveAsync(entity);
        }

        public virtual Task ActiveInactiveAsync(TEntity entity)
        {
            entity.IsActive = !entity.IsActive;
            return UpdateAsync(entity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity != null) await DeleteAsync(entity);
        }

        public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, Guid id)
        {
            var entity = await DbContext.Set<TEntity>()
                .Where(e => e.Id == id).Where(predicate).FirstOrDefaultAsync();
            if (entity != null) await DeleteAsync(entity);
        }

        public virtual Task DeleteAsync(TEntity entity)
        {
            entity.IsDeleted = true;
            return UpdateAsync(entity);
        }

        public virtual async Task DeleteFromDBAsync(Guid id)
        {
            var entity = await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity != null) DbContext.Set<TEntity>().Remove(entity);
        }

        public virtual async Task DeleteFromDBAsync(Expression<Func<TEntity, bool>> predicate, Guid id)
        {
            var entity = await DbContext.Set<TEntity>()
                .Where(e => e.Id == id).Where(predicate).FirstOrDefaultAsync();
            if (entity != null) DbContext.Set<TEntity>().Remove(entity);
        }

        public virtual Task DeleteFromDBAsync(TEntity entity)
        {
            DbContext.Set<TEntity>().Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeFromDBAsync(IEnumerable<TEntity> entities)
        {
            DbContext.Set<TEntity>().RemoveRange(entities);
            return Task.CompletedTask;
        }

        public virtual Task<bool> IsExistsAsync(Expression<Func<TEntity, bool>> predicate) =>
            DbContext.Set<TEntity>().AnyAsync(predicate);

        private IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query, bool includeRelated)
        {
            if (!includeRelated) return query;

            var entityType = DbContext.Model.FindEntityType(typeof(TEntity));
            if (entityType == null) return query;

            foreach (var nav in entityType.GetNavigations())
                query = query.Include(nav.Name);
            return query;
        }
    }
}
