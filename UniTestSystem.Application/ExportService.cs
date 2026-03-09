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
        byte[] ExportFacultyYearPdf(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, SystemSettings settings, DateTime from, DateTime to);
        byte[] ExportStudentSubjectPdf(StudentSubjectReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to);
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
    }
}
