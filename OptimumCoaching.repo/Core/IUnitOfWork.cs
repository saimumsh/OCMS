using Microsoft.EntityFrameworkCore.Storage;

namespace OptimumCoaching.repo.Core
{
    public interface IUnitOfWork
    {
        Task<bool> CompleteAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
