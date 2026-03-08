using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Persistence;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate != null 
            ? await _dbSet.Where(predicate).ToListAsync() 
            : await _dbSet.ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        // Executes as SQL: SELECT TOP 1 ... WHERE ...
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public async Task InsertAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertAsync(Expression<Func<T, bool>> predicate, T entity)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(predicate);
        if (existing == null)
        {
            await _dbSet.AddAsync(entity);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(entity);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync();
        if (entities.Any())
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
