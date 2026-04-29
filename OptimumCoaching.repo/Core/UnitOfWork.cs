using Microsoft.EntityFrameworkCore.Storage;

namespace OptimumCoaching.repo.Core
{
    public class UnitOfWork : IUnitOfWork
    {
        protected readonly ApplicationDbContext DbContext;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<bool> CompleteAsync() =>
            await DbContext.SaveChangesAsync() > 0;

        public Task<IDbContextTransaction> BeginTransactionAsync() =>
            DbContext.Database.BeginTransactionAsync();
    }
}
