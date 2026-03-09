using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniTestSystem.Domain;
using DomainUser = UniTestSystem.Domain.User;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/results")]
public class ResultsController : ControllerBase
{
    public class ExportResultDto
    {
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string TestTitle { get; set; } = "";
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime SubmitTime { get; set; }
        public SessionStatus Status { get; set; }
    }

    private readonly IRepository<Result> _results;
    private readonly IRepository<DomainUser> _users;
    private readonly IRepository<Test> _tests;

    public ResultsController(IRepository<Result> results, IRepository<DomainUser> users, IRepository<Test> tests)
    {
        _results = results;
        _users = users;
        _tests = tests;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? testId = null)
    {
        var results = await _results.GetAllAsync();
        var users = await _users.GetAllAsync();
        var tests = await _tests.GetAllAsync();

        var query = results.AsQueryable();
        if (!string.IsNullOrEmpty(testId))
        {
            query = query.Where(r => r.TestId == testId);
        }

        var resultList = query.Join(users, r => r.UserId, u => u.Id, (r, u) => new { r, u })
                           .Join(tests, ru => ru.r.TestId, t => t.Id, (ru, t) => new {
                               ru.r.Id,
                               UserName = ru.u.Name,
                               UserEmail = ru.u.Email,
                               TestTitle = t.Title,
                               TestType = t.Type,
                               ru.r.Score,
                               ru.r.MaxScore,
                               ru.r.SubmitTime,
                               ru.r.Status
                           })
                           .OrderByDescending(r => r.SubmitTime)
                           .ToList();

        return Ok(resultList);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var results = await _results.GetAllAsync();
        var tests = await _tests.GetAllAsync();

        var stats = new {
            TotalSubmissions = results.Count,
            AverageScore = results.Any() ? results.Average(r => r.Score) : 0,
            PassRate = results.Any() ? (decimal)results.Count(r => r.Score >= 5) / results.Count * 100 : 0,
            TestsByType = tests.GroupBy(t => t.Type)
                              .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            SubmissionsByMonth = results.GroupBy(r => new { r.SubmitTime.Year, r.SubmitTime.Month })
                                        .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Count())
        };

        return Ok(stats);
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] string? testId = null)
    {
        var results = await GetResultData(testId);
        
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Test Results");
        
        worksheet.Cell(1, 1).Value = "User Name";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(1, 3).Value = "Test Title";
        worksheet.Cell(1, 4).Value = "Score";
        worksheet.Cell(1, 5).Value = "Max Score";
        worksheet.Cell(1, 6).Value = "Submit Time";
        worksheet.Cell(1, 7).Value = "Status";
        
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            worksheet.Cell(i + 2, 1).Value = r.UserName;
            worksheet.Cell(i + 2, 2).Value = r.UserEmail;
            worksheet.Cell(i + 2, 3).Value = r.TestTitle;
            worksheet.Cell(i + 2, 4).Value = r.Score;
            worksheet.Cell(i + 2, 5).Value = r.MaxScore;
            worksheet.Cell(i + 2, 6).Value = r.SubmitTime;
            worksheet.Cell(i + 2, 7).Value = r.Status.ToString();
        }
        
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Results_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] string? testId = null)
    {
        var results = await GetResultData(testId);
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.Header().Text("UniTestSystem - Results Report").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                
                page.Content().PaddingVertical(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("Name").Bold();
                        header.Cell().Text("Test").Bold();
                        header.Cell().Text("Score").Bold();
                        header.Cell().Text("Date").Bold();
                    });
                    
                    foreach (var r in results)
                    {
                        table.Cell().Text(r.UserName);
                        table.Cell().Text(r.TestTitle);
                        table.Cell().Text($"{r.Score}/{r.MaxScore}");
                        table.Cell().Text(r.SubmitTime.ToString("dd/MM/yyyy"));
                    }
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", $"Results_{DateTime.Now:yyyyMMdd}.pdf");
    }

    private async Task<List<ExportResultDto>> GetResultData(string? testId)
    {
        var rs = await _results.GetAllAsync();
        var us = await _users.GetAllAsync();
        var ts = await _tests.GetAllAsync();

        var query = rs.AsQueryable();
        if (!string.IsNullOrEmpty(testId)) query = query.Where(r => r.TestId == testId);

        return query.Join(us, r => r.UserId, u => u.Id, (r, u) => new { r, u })
                    .Join(ts, ru => ru.r.TestId, t => t.Id, (ru, t) => new ExportResultDto {
                        UserName = ru.u.Name,
                        UserEmail = ru.u.Email,
                        TestTitle = t.Title,
                        Score = ru.r.Score,
                        MaxScore = ru.r.MaxScore,
                        SubmitTime = ru.r.SubmitTime,
                        Status = ru.r.Status
                    })
                    .ToList();
    }
}
