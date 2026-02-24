using System;

namespace Employee_Survey.Models
{
    public class RoleReportVm
    {
        public List<RoleReportRow> Rows { get; set; } = new();
    }

    public class RoleReportRow
    {
        public string Role { get; set; } = "";
        public int UserCount { get; set; }
        public int SubmissionCount { get; set; }
        public double AvgScore { get; set; }
        public double PassRatePercent { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }

    public class LevelReportVm
    {
        public List<LevelReportRow> Rows { get; set; } = new();
    }

    public class LevelReportRow
    {
        public string Level { get; set; } = "";
        public int UserCount { get; set; }
        public int SubmissionCount { get; set; }
        public double AvgScore { get; set; }
        public double PassRatePercent { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }

    public class UserSkillReportVm
    {
        public List<UserSkillReportRow> Rows { get; set; } = new();
    }

    public class UserSkillReportRow
    {
        public string Skill { get; set; } = "";
        public int QuestionCount { get; set; }
        public double TotalScore { get; set; }
        public double AvgPerQuestion { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }
}
