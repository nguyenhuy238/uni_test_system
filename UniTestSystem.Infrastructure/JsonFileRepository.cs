using System.Linq.Expressions;
using System.Text.Json;
using UniTestSystem.Application.Interfaces;
using System.IO;
using System.Linq;

namespace UniTestSystem.Infrastructure
{
    public class JsonFileRepository<T> : IRepository<T> where T : class
    {
        private readonly string _path;
        private readonly JsonSerializerOptions _opt = new() { WriteIndented = true };
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public JsonFileRepository()
        {
            var folder = "App_Data";
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, $"{typeof(T).Name}.json");
            if (!File.Exists(_path)) File.WriteAllText(_path, "[]");
        }

        private async Task<List<T>> ReadAsync()
        {
            var json = await File.ReadAllTextAsync(_path);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new();
        }
        private async Task WriteAsync(List<T> data)
        {
            var json = JsonSerializer.Serialize(data, _opt);
            await File.WriteAllTextAsync(_path, json);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
        {
            await _lock.WaitAsync();
            try 
            { 
                var list = await ReadAsync();
                return predicate == null ? list : list.Where(predicate.Compile()).ToList();
            }
            finally { _lock.Release(); }
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            await _lock.WaitAsync();
            try { return (await ReadAsync()).FirstOrDefault(predicate.Compile()); }
            finally { _lock.Release(); }
        }

        public IQueryable<T> Query()
        {
            // For JSON file repository, we load all into memory for Query()
            // In a real high-load app, this repo should be replaced by EF/SQL
            return ReadAsync().GetAwaiter().GetResult().AsQueryable();
        }

        public async Task InsertAsync(T entity)
        {
            await _lock.WaitAsync();
            try { var list = await ReadAsync(); list.Add(entity); await WriteAsync(list); }
            finally { _lock.Release(); }
        }

        public async Task UpdateAsync(T entity)
        {
            // Simple update: assumes the entity is already in the list (or has an ID match)
            // For Json repo, we might need a more robust way to find/replace
            await _lock.WaitAsync();
            try
            {
                var list = await ReadAsync();
                // This is a naive implementation; usually we'd match by Id
                await WriteAsync(list); // Just re-writing for now as placeholder
            }
            finally { _lock.Release(); }
        }

        public async Task UpsertAsync(Expression<Func<T, bool>> predicate, T entity)
        {
            await _lock.WaitAsync();
            try
            {
                var list = await ReadAsync();
                var compiled = predicate.Compile();
                var i = list.FindIndex(x => compiled(x));
                if (i >= 0) list[i] = entity; else list.Add(entity);
                await WriteAsync(list);
            }
            finally { _lock.Release(); }
        }

        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            await _lock.WaitAsync();
            try
            {
                var list = await ReadAsync();
                var compiled = predicate.Compile();
                list.RemoveAll(x => compiled(x));
                await WriteAsync(list);
            }
            finally { _lock.Release(); }
        }
    }
}
