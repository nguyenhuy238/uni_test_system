using Microsoft.EntityFrameworkCore;
using Employee_Survey.Infrastructure;

namespace Employee_Survey.Infrastructure.Persistence;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate)
    {
        // Note: Using Func instead of Expression means this executes in-memory after fetching some/all records.
        // For now, mapping to AsEnumerable() to satisfy the interface.
        return await Task.Run(() => _dbSet.AsEnumerable().FirstOrDefault(predicate));
    }

    public async Task InsertAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertAsync(Func<T, bool> predicate, T entity)
    {
        var existing = _dbSet.AsEnumerable().FirstOrDefault(predicate);
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

    public async Task DeleteAsync(Func<T, bool> predicate)
    {
        var entities = _dbSet.AsEnumerable().Where(predicate).ToList();
        if (entities.Any())
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
