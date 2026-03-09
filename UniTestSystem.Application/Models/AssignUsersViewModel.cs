using System.Collections.Generic;
using UniTestSystem.Domain;

namespace UniTestSystem.Application.Models
{
    public class AssignUsersViewModel
    {
        public string TestId { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;

        // Danh sách user hiển thị (đã lọc theo Faculty nếu có)
        public List<User> Users { get; set; } = new();

        // Những user đã được assign sẵn
        public HashSet<string> AssignedUserIds { get; set; } = new();

        // Bộ lọc Faculty
        public List<string> Faculties { get; set; } = new();
        public string? SelectedFaculty { get; set; }
    }
}
