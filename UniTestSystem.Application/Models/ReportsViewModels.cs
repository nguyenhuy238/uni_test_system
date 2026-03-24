using System;

namespace UniTestSystem.Application.Models
{
    public class ReportsIndexVm
    {
        public FacultyReportVm Faculty { get; set; } = new();
        public AcademicYearReportVm AcademicYear { get; set; } = new();
        public WidgetDashboardVm Dashboard { get; set; } = new();
    }

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

    public class WidgetDashboardVm
    {
        public int SubmissionCount { get; set; }
        public decimal OverallAvgScore { get; set; }
        public decimal OverallPassRatePercent { get; set; }
        public List<SubjectPassRateRow> SubjectPassRates { get; set; } = new();
        public List<SemesterAverageRow> SemesterAverages { get; set; } = new();
        public List<ScoreDistributionBucketRow> ScoreDistribution { get; set; } = new();
    }

    public class SubjectPassRateRow
    {
        public string Subject { get; set; } = "";
        public int SubmissionCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public decimal PassRatePercent { get; set; }
        public decimal AvgScore { get; set; }
    }

    public class SemesterAverageRow
    {
        public string Semester { get; set; } = "";
        public int SubmissionCount { get; set; }
        public decimal AvgScore { get; set; }
    }

    public class ScoreDistributionBucketRow
    {
        public string BucketLabel { get; set; } = "";
        public int Count { get; set; }
        public decimal Percent { get; set; }
    }

    public class QuestionAnalyticsVm
    {
        public int TotalQuestions { get; set; }
        public int HardQuestions { get; set; }
        public int MediumQuestions { get; set; }
        public int EasyQuestions { get; set; }
        public int LowDiscriminationQuestions { get; set; }
        public List<QuestionAnalyticsRow> Rows { get; set; } = new();
    }

    public class QuestionAnalyticsRow
    {
        public string QuestionId { get; set; } = "";
        public string ContentPreview { get; set; } = "";
        public string Type { get; set; } = "";
        public string Subject { get; set; } = "";
        public int Attempts { get; set; }
        public decimal AvgScorePercent { get; set; }
        public string DifficultyLabel { get; set; } = "";
        public decimal DiscriminationIndex { get; set; }
        public string DiscriminationLabel { get; set; } = "";
    }

    public class LecturerPerformanceVm
    {
        public List<LecturerPerformanceRow> Rows { get; set; } = new();
    }

    public class LecturerPerformanceRow
    {
        public string LecturerId { get; set; } = "";
        public string LecturerName { get; set; } = "";
        public int CourseCount { get; set; }
        public int TestCount { get; set; }
        public int SubmissionCount { get; set; }
        public decimal AvgScore { get; set; }
        public decimal PassRatePercent { get; set; }
        public DateTime? LastSubmissionAt { get; set; }
    }
}
