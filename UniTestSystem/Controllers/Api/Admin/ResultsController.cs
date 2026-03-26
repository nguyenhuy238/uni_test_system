using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/results")]
public class ResultsController : ControllerBase
{
    private readonly IResultsService _resultsService;

    public ResultsController(IResultsService resultsService)
    {
        _resultsService = resultsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? testId = null)
    {
        return Ok(await _resultsService.GetResultsAsync(testId));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(await _resultsService.GetStatsAsync());
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] string? testId = null)
    {
        var results = await _resultsService.GetExportDataAsync(testId);
        
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
        var results = await _resultsService.GetExportDataAsync(testId);
        
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

}

