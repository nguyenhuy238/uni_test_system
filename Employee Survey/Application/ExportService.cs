using ClosedXML.Excel;
using Employee_Survey.Domain;
using Employee_Survey.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Employee_Survey.Application
{
    public interface IExportService
    {
        byte[] ExportRoleLevelExcel(RoleReportVm roleVm, LevelReportVm levelVm, DateTime from, DateTime to);
        byte[] ExportUserSkillExcel(UserSkillReportVm vm, string userName, DateTime from, DateTime to);
        byte[] ExportRoleLevelPdf(RoleReportVm roleVm, LevelReportVm levelVm, SystemSettings settings, DateTime from, DateTime to);
        byte[] ExportUserSkillPdf(UserSkillReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to);
    }

    public class ExportService : IExportService
    {
        public byte[] ExportRoleLevelExcel(RoleReportVm roleVm, LevelReportVm levelVm, DateTime from, DateTime to)
        {
            using var wb = new XLWorkbook();

            var ws1 = wb.AddWorksheet("By Role");
            ws1.Cell(1, 1).Value = $"Role Report ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws1.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws1.Cell(3, 1).Value = "Role";
            ws1.Cell(3, 2).Value = "#Users";
            ws1.Cell(3, 3).Value = "#Submissions";
            ws1.Cell(3, 4).Value = "AvgScore";
            ws1.Cell(3, 5).Value = "PassRate%";
            ws1.Cell(3, 6).Value = "LastSubmission";

            int r = 4;
            foreach (var row in roleVm.Rows)
            {
                ws1.Cell(r, 1).Value = row.Role;
                ws1.Cell(r, 2).Value = row.UserCount;
                ws1.Cell(r, 3).Value = row.SubmissionCount;
                ws1.Cell(r, 4).Value = row.AvgScore;
                ws1.Cell(r, 5).Value = row.PassRatePercent;
                ws1.Cell(r, 6).Value = row.LastSubmissionAt;
                r++;
            }
            ws1.Columns().AdjustToContents();

            var ws2 = wb.AddWorksheet("By Level");
            ws2.Cell(1, 1).Value = $"Level Report ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws2.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws2.Cell(3, 1).Value = "Level";
            ws2.Cell(3, 2).Value = "#Users";
            ws2.Cell(3, 3).Value = "#Submissions";
            ws2.Cell(3, 4).Value = "AvgScore";
            ws2.Cell(3, 5).Value = "PassRate%";
            ws2.Cell(3, 6).Value = "LastSubmission";

            r = 4;
            foreach (var row in levelVm.Rows)
            {
                ws2.Cell(r, 1).Value = row.Level;
                ws2.Cell(r, 2).Value = row.UserCount;
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

        public byte[] ExportUserSkillExcel(UserSkillReportVm vm, string userName, DateTime from, DateTime to)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("User Skills");
            ws.Cell(1, 1).Value = $"User Skill Report - {userName} ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})";
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.FontSize = 14;

            ws.Cell(3, 1).Value = "Skill";
            ws.Cell(3, 2).Value = "#Questions";
            ws.Cell(3, 3).Value = "TotalScore";
            ws.Cell(3, 4).Value = "AvgScorePerQuestion";
            ws.Cell(3, 5).Value = "LastSubmission";

            int r = 4;
            foreach (var row in vm.Rows)
            {
                ws.Cell(r, 1).Value = row.Skill;
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

        public byte[] ExportRoleLevelPdf(RoleReportVm roleVm, LevelReportVm levelVm, SystemSettings settings, DateTime from, DateTime to)
        {
            var title = settings.SystemName ?? "Employee Survey";
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Reports ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})").FontSize(12);

                        col.Item().PaddingTop(10).Text("By Role").Bold(); // ✅ PaddingTop đặt TRƯỚC Text
                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Role", "#Users", "#Submissions", "AvgScore", "PassRate%", "LastSubmission");
                            foreach (var row in roleVm.Rows)
                                TableRow(c, row.Role, row.UserCount.ToString(), row.SubmissionCount.ToString(),
                                    row.AvgScore.ToString("0.##"),
                                    row.PassRatePercent.ToString("0.#"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                        });

                        col.Item().PaddingTop(16).Text("By Level").Bold(); // ✅
                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Level", "#Users", "#Submissions", "AvgScore", "PassRate%", "LastSubmission");
                            foreach (var row in levelVm.Rows)
                                TableRow(c, row.Level, row.UserCount.ToString(), row.SubmissionCount.ToString(),
                                    row.AvgScore.ToString("0.##"),
                                    row.PassRatePercent.ToString("0.#"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }

        public byte[] ExportUserSkillPdf(UserSkillReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to)
        {
            var title = settings.SystemName ?? "Employee Survey";
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"User Skill Report - {userName} ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})").Bold();

                        col.Item().Element(c =>
                        {
                            TableHeader(c, "Skill", "#Questions", "TotalScore", "AvgScore/Question", "LastSubmission");
                            foreach (var row in vm.Rows)
                                TableRow(c, row.Skill, row.QuestionCount.ToString(),
                                    row.TotalScore.ToString("0.##"),
                                    row.AvgPerQuestion.ToString("0.##"),
                                    row.LastSubmissionAt?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            return doc.GeneratePdf();
        }
    }
}
