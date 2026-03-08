using System;

namespace UniTestSystem.Application.Interfaces
{
    public class AuditEntryDto
    {
        public int Id { get; set; }
        public DateTime At { get; set; }
        public string Actor { get; set; } = "";
        public string Action { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string? Before { get; set; }
        public string? After { get; set; }
    }
}
