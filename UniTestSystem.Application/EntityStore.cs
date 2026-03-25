using System.Linq.Expressions;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Application;

public sealed class EntityStore<T> : IEntityStore<T> where T : class
{
    private readonly IRepository<T> _repository;

    public EntityStore(IRepository<T> repository)
    {
        _repository = repository;
    }

    public Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return _repository.GetAllAsync(predicate);
    }

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return _repository.FirstOrDefaultAsync(predicate);
    }

    public Task<List<T>> ListAsync(ISpecification<T>? specification = null)
    {
        return _repository.ListAsync(specification);
    }

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification)
    {
        return _repository.FirstOrDefaultAsync(specification);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return _repository.CountAsync(predicate);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return _repository.AnyAsync(predicate);
    }

    public Task InsertAsync(T entity)
    {
        return _repository.InsertAsync(entity);
    }

    public Task UpdateAsync(T entity)
    {
        return _repository.UpdateAsync(entity);
    }

    public Task UpsertAsync(Expression<Func<T, bool>> predicate, T entity)
    {
        return _repository.UpsertAsync(predicate, entity);
    }

    public Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        return _repository.DeleteAsync(predicate);
    }
}
