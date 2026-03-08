using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniTestSystem.Application.Interfaces
{
    public interface IAuditReaderService
    {
        Task<List<AuditEntryDto>> GetAllAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? keyword = null, string? actor = null);
    }
}
