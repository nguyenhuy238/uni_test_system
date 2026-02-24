using System.Text.Json;
using Employee_Survey.Domain;
using Microsoft.Extensions.Configuration;

namespace Employee_Survey.Application
{
    public interface ISettingsService
    {
        Task<SystemSettings> GetAsync();
        Task SaveAsync(SystemSettings s);
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _path;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private readonly JsonSerializerOptions _opt = new() { WriteIndented = true };

        public SettingsService(IConfiguration cfg)
        {
            var folder = cfg["DataFolder"] ?? "App_Data";
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, "SystemSettings.json");
            if (!File.Exists(_path))
            {
                var def = new SystemSettings();
                File.WriteAllText(_path, JsonSerializer.Serialize(def, _opt));
            }
        }

        public async Task<SystemSettings> GetAsync()
        {
            await _lock.WaitAsync();
            try
            {
                var json = await File.ReadAllTextAsync(_path);
                return JsonSerializer.Deserialize<SystemSettings>(json) ?? new SystemSettings();
            }
            finally { _lock.Release(); }
        }

        public async Task SaveAsync(SystemSettings s)
        {
            await _lock.WaitAsync();
            try
            {
                s.UpdatedAt = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(s, _opt);
                await File.WriteAllTextAsync(_path, json);
            }
            finally { _lock.Release(); }
        }
    }
}
