using System.Linq.Expressions;

namespace UniTestSystem.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> Query();
        Task InsertAsync(T entity);
        Task UpdateAsync(T entity);
        Task UpsertAsync(Expression<Func<T, bool>> predicate, T entity);
        Task DeleteAsync(Expression<Func<T, bool>> predicate);
    }
}
