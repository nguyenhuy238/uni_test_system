using System;

namespace UniTestSystem.Domain
{
    public class AuditEntry
    {
        public int Id { get; set; }
        public DateTime At { get; set; }
        public string Actor { get; set; } = "";
        public string Action { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string? Before { get; set; } // Stored as JSON string
        public string? After { get; set; }  // Stored as JSON string
    }
}
