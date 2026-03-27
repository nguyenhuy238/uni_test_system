using System.Linq.Expressions;

namespace UniTestSystem.Application.Interfaces;

public interface IEntityStore<T> where T : class
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> ListAsync(ISpecification<T>? specification = null);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task InsertAsync(T entity);
    Task UpdateAsync(T entity);
    Task UpsertAsync(Expression<Func<T, bool>> predicate, T entity);
    Task DeleteAsync(Expression<Func<T, bool>> predicate);
}
