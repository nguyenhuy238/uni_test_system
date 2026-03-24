using ClosedXML.Excel;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace UniTestSystem.Application
{
    public interface IExportService
    {
        byte[] ExportFacultyYearExcel(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, DateTime from, DateTime to);
        byte[] ExportStudentSubjectExcel(StudentSubjectReportVm vm, string userName, DateTime from, DateTime to);
        byte[] ExportTranscriptOverviewExcel(IEnumerable<TranscriptAdminRowVm> rows, string? facultyName, string? className, string? semester);
        byte[] ExportStudentTranscriptExcel(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId);
        byte[] ExportFacultyYearPdf(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, SystemSettings settings, DateTime from, DateTime to);
        byte[] ExportStudentSubjectPdf(StudentSubjectReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to);
        byte[] ExportTranscriptOverviewPdf(IEnumerable<TranscriptAdminRowVm> rows, SystemSettings settings, string? facultyName, string? className, string? semester);
        byte[] ExportStudentTranscriptPdf(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId, SystemSettings settings);
    }

    public class ExportService : IExportService
    {
        public byte[] ExportFacultyYearExcel(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, DateTime from, DateTime to)
        {
            using var wb = new XLWorkbook();

            var ws1 = wb.AddWorksheet("By Faculty");
            ws1.Cell(1, 1).Value = $"Faculty Report ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws1.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws1.Cell(3, 1).Value = "Faculty";
            ws1.Cell(3, 2).Value = "#Students";
            ws1.Cell(3, 3).Value = "#Submissions";
            ws1.Cell(3, 4).Value = "AvgScore";
            ws1.Cell(3, 5).Value = "PassRate%";
            ws1.Cell(3, 6).Value = "LastSubmission";

            int r = 4;
            foreach (var row in facultyVm.Rows)
            {
                ws1.Cell(r, 1).Value = row.FacultyName;
                ws1.Cell(r, 2).Value = row.StudentCount;
                ws1.Cell(r, 3).Value = row.SubmissionCount;
                ws1.Cell(r, 4).Value = row.AvgScore;
                ws1.Cell(r, 5).Value = row.PassRatePercent;
                ws1.Cell(r, 6).Value = row.LastSubmissionAt;
                r++;
            }
            ws1.Columns().AdjustToContents();

            var ws2 = wb.AddWorksheet("By Academic Year");
            ws2.Cell(1, 1).Value = $"Academic Year Report ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws2.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws2.Cell(3, 1).Value = "Year";
            ws2.Cell(3, 2).Value = "#Students";
            ws2.Cell(3, 3).Value = "#Submissions";
            ws2.Cell(3, 4).Value = "AvgScore";
            ws2.Cell(3, 5).Value = "PassRate%";
            ws2.Cell(3, 6).Value = "LastSubmission";

            r = 4;
            foreach (var row in yearVm.Rows)
            {
                ws2.Cell(r, 1).Value = row.AcademicYear;
                ws2.Cell(r, 2).Value = row.StudentCount;
                ws2.Cell(r, 3).Value = row.SubmissionCount;
                ws2.Cell(r, 4).Value = row.AvgScore;
                ws2.Cell(r, 5).Value = row.PassRatePercent;
                ws2.Cell(r, 6).Value = row.LastSubmissionAt;
                r++;
            }
            ws2.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportStudentSubjectExcel(StudentSubjectReportVm vm, string userName, DateTime from, DateTime to)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Student Subjects");
            ws.Cell(1, 1).Value = $"Student Subject Report - {userName} ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws.Cell(3, 1).Value = "Subject";
            ws.Cell(3, 2).Value = "#Questions";
            ws.Cell(3, 3).Value = "TotalScore";
            ws.Cell(3, 4).Value = "AvgScore/Question";
            ws.Cell(3, 5).Value = "LastSubmission";

            int r = 4;
            foreach (var row in vm.Rows)
            {
                ws.Cell(r, 1).Value = row.Subject;
                ws.Cell(r, 2).Value = row.QuestionCount;
                ws.Cell(r, 3).Value = row.TotalScore;
                ws.Cell(r, 4).Value = row.AvgPerQuestion;
                ws.Cell(r, 5).Value = row.LastSubmissionAt;
                r++;
            }
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportTranscriptOverviewExcel(IEnumerable<TranscriptAdminRowVm> rows, string? facultyName, string? className, string? semester)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Transcripts");

            ws.Cell(1, 1).Value = "Transcript Overview";
            ws.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws.Cell(2, 1).Value = $"Faculty: {facultyName ?? "All"}";
            ws.Cell(2, 3).Value = $"Class: {className ?? "All"}";
            ws.Cell(2, 5).Value = $"Semester: {semester ?? "All"}";

            ws.Cell(4, 1).Value = "Student ID";
            ws.Cell(4, 2).Value = "Student Name";
            ws.Cell(4, 3).Value = "Faculty";
            ws.Cell(4, 4).Value = "Class";
            ws.Cell(4, 5).Value = "GPA";
            ws.Cell(4, 6).Value = "Total Credits";
            ws.Cell(4, 7).Value = "Calculated At (UTC)";

            int r = 5;
            foreach (var row in rows)
            {
                ws.Cell(r, 1).Value = row.StudentId;
                ws.Cell(r, 2).Value = row.StudentName;
                ws.Cell(r, 3).Value = row.FacultyName;
                ws.Cell(r, 4).Value = row.ClassName;
                ws.Cell(r, 5).Value = row.GPA;
                ws.Cell(r, 6).Value = row.TotalCredits;
                ws.Cell(r, 7).Value = row.CalculatedAt;
                r++;
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportStudentTranscriptExcel(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Student Transcript");
            ws.Cell(1, 1).Value = $"Student Transcript - {studentName} ({studentId})";
            ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws.Cell(2, 1).Value = "Cumulative GPA";
            ws.Cell(2, 2).Value = summary?.GPA ?? 0m;
            ws.Cell(2, 4).Value = "Total Credits";
            ws.Cell(2, 5).Value = summary?.TotalCredits ?? 0;

            ws.Cell(4, 1).Value = "Semester";
            ws.Cell(4, 2).Value = "Course Code";
            ws.Cell(4, 3).Value = "Course Name";
            ws.Cell(4, 4).Value = "Credits";
            ws.Cell(4, 5).Value = "Final Score";
            ws.Cell(4, 6).Value = "Grade";
            ws.Cell(4, 7).Value = "Grade Point";

            int r = 5;
            foreach (var g in grades.OrderBy(x => x.Semester).ThenBy(x => x.Course?.Code))
            {
                ws.Cell(r, 1).Value = g.Semester;
                ws.Cell(r, 2).Value = g.Course?.Code ?? "";
                ws.Cell(r, 3).Value = g.Course?.Name ?? "";
                ws.Cell(r, 4).Value = g.Course?.Credits ?? 0;
                ws.Cell(r, 5).Value = g.FinalScore;
                ws.Cell(r, 6).Value = g.Grade ?? "";
                ws.Cell(r, 7).Value = g.GradePoint;
                r++;
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // PDF helpers
        private static void TableHeader(IContainer c, params string[] headers)
        {
            c.Row(row =>
            {
                foreach (var h in headers)
                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(6).Text(h).SemiBold();
            });
        }

        private static void TableRow(IContainer c, params string[] cells)
        {
            c.Row(row =>
            {
                foreach (var t in cells)
                    row.RelativeItem().Padding(6).Text(t);
            });
        }

        public byte[] ExportFacultyYearPdf(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, SystemSettings settings, DateTime from, DateTime to)
        {
            var title = settings.SystemName ?? "UniTestSystem";
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Academic Reports ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})").FontSize(12);

                        col.Item().PaddingTop(10).Text("By Faculty").Bold();
                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Faculty", "#Students", "#Submits", "Avg", "Pass%", "Last");
                            foreach (var row in facultyVm.Rows)
                                TableRow(c, row.FacultyName, row.StudentCount.ToString(), row.SubmissionCount.ToString(),
                                    row.AvgScore.ToString("0.##"),
                                    row.PassRatePercent.ToString("0.#"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd") ?? "-");
                        });

                        col.Item().PaddingTop(16).Text("By Academic Year").Bold();
                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Year", "#Students", "#Submits", "Avg", "Pass%", "Last");
                            foreach (var row in yearVm.Rows)
                                TableRow(c, row.AcademicYear, row.StudentCount.ToString(), row.SubmissionCount.ToString(),
                                    row.AvgScore.ToString("0.##"),
                                    row.PassRatePercent.ToString("0.#"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd") ?? "-");
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }

        public byte[] ExportStudentSubjectPdf(StudentSubjectReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to)
        {
            var title = settings.SystemName ?? "UniTestSystem";
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Student Subject Report - {userName} ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})").Bold();

                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Subject", "#Qs", "Total", "Avg/Q", "Last");
                            foreach (var row in vm.Rows)
                                TableRow(c, row.Subject, row.QuestionCount.ToString(),
                                    row.TotalScore.ToString("0.##"),
                                    row.AvgPerQuestion.ToString("0.##"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd") ?? "-");
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }

        public byte[] ExportTranscriptOverviewPdf(IEnumerable<TranscriptAdminRowVm> rows, SystemSettings settings, string? facultyName, string? className, string? semester)
        {
            var title = settings.SystemName ?? "UniTestSystem";
            var list = rows.ToList();
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Transcript Overview").Bold();
                        col.Item().Text($"Faculty: {facultyName ?? "All"} | Class: {className ?? "All"} | Semester: {semester ?? "All"}");

                        col.Item().PaddingTop(10).Element(c =>
                        {
                            TableHeader(c, "Student", "Faculty", "Class", "GPA", "Credits", "Calculated");
                            foreach (var row in list)
                                TableRow(
                                    c,
                                    $"{row.StudentName} ({row.StudentId})",
                                    row.FacultyName,
                                    row.ClassName,
                                    row.GPA.ToString("0.00"),
                                    row.TotalCredits.ToString(),
                                    row.CalculatedAt.ToString("yyyy-MM-dd"));
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }

        public byte[] ExportStudentTranscriptPdf(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId, SystemSettings settings)
        {
            var title = settings.SystemName ?? "UniTestSystem";
            var gradeList = grades.OrderBy(g => g.Semester).ThenBy(g => g.Course?.Code).ToList();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Student Transcript - {studentName} ({studentId})").Bold();
                        col.Item().Text($"Cumulative GPA: {(summary?.GPA ?? 0m):0.00} | Total Credits: {summary?.TotalCredits ?? 0}");

                        col.Item().PaddingTop(10).Element(c =>
                        {
                            TableHeader(c, "Semester", "Course", "Credits", "Score", "Grade", "Point");
                            foreach (var g in gradeList)
                                TableRow(
                                    c,
                                    g.Semester,
                                    $"{g.Course?.Code} - {g.Course?.Name}",
                                    (g.Course?.Credits ?? 0).ToString(),
                                    g.FinalScore?.ToString("0.0") ?? "-",
                                    g.Grade ?? "-",
                                    g.GradePoint?.ToString("0.0") ?? "-");
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }
    }
}
