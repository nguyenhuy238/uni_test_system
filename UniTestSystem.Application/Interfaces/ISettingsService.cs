using System;
using System.Threading.Tasks;
using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces
{
    public interface ISettingsService
    {
        Task<SystemSettings> GetAsync();
        Task SaveAsync(SystemSettings s);
    }
}
