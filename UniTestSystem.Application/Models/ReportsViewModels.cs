using System;

namespace UniTestSystem.Application.Models
{
    public class FacultyReportVm
    {
        public List<FacultyReportRow> Rows { get; set; } = new();
    }

    public class FacultyReportRow
    {
        public string FacultyName { get; set; } = "";
        public int StudentCount { get; set; }
        public int SubmissionCount { get; set; }
        public decimal AvgScore { get; set; }
        public decimal PassRatePercent { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }

    public class AcademicYearReportVm
    {
        public List<AcademicYearReportRow> Rows { get; set; } = new();
    }

    public class AcademicYearReportRow
    {
        public string AcademicYear { get; set; } = "";
        public int StudentCount { get; set; }
        public int SubmissionCount { get; set; }
        public decimal AvgScore { get; set; }
        public decimal PassRatePercent { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }

    public class StudentSubjectReportVm
    {
        public List<StudentSubjectReportRow> Rows { get; set; } = new();
    }

    public class StudentSubjectReportRow
    {
        public string Subject { get; set; } = "";
        public int QuestionCount { get; set; }
        public decimal TotalScore { get; set; }
        public decimal AvgPerQuestion { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }
}
