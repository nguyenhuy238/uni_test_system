using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers;

[Authorize(Policy = PermissionCodes.Question_View)]
public class QuestionsController : Controller
{
    private readonly IQuestionService _svc;
    private readonly IQuestionExcelService _xlsx;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;

    public QuestionsController(
        IQuestionService svc,
        IQuestionExcelService xlsx,
        IWebHostEnvironment env,
        IConfiguration cfg)
    {
        _svc = svc;
        _xlsx = xlsx;
        _env = env;
        _cfg = cfg;
    }

    // LIST + FILTER + PAGING
    public async Task<IActionResult> Index([FromQuery] QuestionFilter f)
    {
        var result = await _svc.SearchAsync(f);
        return View(result);
    }

    // CREATE
    [HttpGet]
    [Authorize(Policy = PermissionCodes.Question_Create)]
    public IActionResult Create()
        => View(new Question
        {
            Type = QType.MCQ,
            Options = new List<Option>
            {
                new Option { Content = "A", IsCorrect = true },
                new Option { Content = "B", IsCorrect = false },
                new Option { Content = "C", IsCorrect = false },
                new Option { Content = "D", IsCorrect = false }
            }
        });

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Create)]
    public async Task<IActionResult> Create(
        Question q,
        List<string>? Options, // textarea name is Options
        List<IFormFile>? mediaFiles,
        string? CorrectKeys,
        string? MatchingPairsRaw,
        string? DragTokens,
        string? DragSlotsRaw,
        string? TagsCsv)
    {
        try
        {
            var media = mediaFiles?.Any() == true ? await SaveMediaAsync(mediaFiles) : null;
            var id = await _svc.CreateFromFormAsync(new QuestionFormCommand
            {
                Question = q,
                Options = Options,
                CorrectKeys = CorrectKeys,
                MatchingPairsRaw = MatchingPairsRaw,
                DragTokens = DragTokens,
                DragSlotsRaw = DragSlotsRaw,
                TagsCsv = TagsCsv,
                NewMedia = media
            }, User.Identity?.Name ?? "hr");

            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(q);
        }
    }

    // ===== EDIT (ROUTES RÕ RÀNG) =====
    // GET: /Questions/Edit/{id}
    [HttpGet("/Questions/Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var editData = await _svc.GetEditDataAsync(id);
        if (editData == null) return NotFound();

        ViewBag.QuestionVersions = editData.Versions;
        return View(editData.Question);
    }

    // POST: /Questions/Edit/{id}
    [HttpPost("/Questions/Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Edit(
        string id,                     // id từ route
        Question q,                    // dữ liệu post từ form
        List<string>? Options,         // textarea name is Options
        List<IFormFile>? mediaFiles,
        string? CorrectKeys,
        string? MatchingPairsRaw,
        string? DragTokens,
        string? DragSlotsRaw,
        string? TagsCsv)
    {
        if (!ModelState.IsValid)
            return View(q);

        try
        {
            var media = mediaFiles?.Any() == true ? await SaveMediaAsync(mediaFiles) : null;
            var (success, reason) = await _svc.UpdateFromFormAsync(new QuestionFormCommand
            {
                Id = id,
                Question = q,
                Options = Options,
                CorrectKeys = CorrectKeys,
                MatchingPairsRaw = MatchingPairsRaw,
                DragTokens = DragTokens,
                DragSlotsRaw = DragSlotsRaw,
                TagsCsv = TagsCsv,
                NewMedia = media
            }, User.Identity?.Name ?? "hr");

            if (!success)
            {
                ModelState.AddModelError("", reason ?? "Update failed");
                return View(q);
            }
            TempData["Msg"] = "Cập nhật thành công";
            return RedirectToAction(nameof(Edit), new { id = q.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(q);
        }
    }
    // ===== END EDIT =====

    public IActionResult DetailsBlocked(string id, string reason) { ViewBag.Reason = reason; ViewBag.Id = id; return View(); }

    // CLONE
    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Create)]
    public async Task<IActionResult> Clone(string id)
    {
        var newId = await _svc.CloneAsync(id, User.Identity?.Name ?? "hr");
        return RedirectToAction(nameof(Edit), new { id = newId });
    }

    // IMPORT
    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Err"] = "Chọn file Excel";
            return RedirectToAction(nameof(Index));
        }
        using var s = file.OpenReadStream();
        var res = await _xlsx.ImportAsync(s, User.Identity?.Name ?? "hr");

        TempData["Msg"] = $"Imported {res.Success}/{res.Total}. Skipped: {res.Skipped}. Errors: {res.Errors.Count}";
        if (res.Errors.Any())
            TempData["Err"] = $"Import có lỗi: {res.Errors.Count} dòng thất bại.";
        else if (res.SkippedReasons.Any())
            TempData["Info"] = $"Có {res.SkippedReasons.Count} dòng bị bỏ qua.";
        if (res.Errors.Any() || res.SkippedReasons.Any())
            TempData["ErrDetail"] = string.Join("\n", res.Errors.Concat(res.SkippedReasons));
        return RedirectToAction(nameof(Index));
    }

    // EXPORT
    [HttpGet]
    public async Task<FileResult> ExportExcel()
    {
        var all = await _svc.GetAllAsync();
        var bytes = await _xlsx.ExportAsync(all);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "QuestionBank.xlsx");
    }

    // DELETE QUESTION
    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var (success, reason) = await _svc.DeleteAsync(id, User.Identity?.Name ?? "hr");
        if (!success)
        {
            TempData["Err"] = reason ?? "Xóa thất bại";
            return RedirectToAction(nameof(Edit), new { id });
        }
        TempData["Msg"] = "Đã xóa câu hỏi";
        return RedirectToAction(nameof(Index));
    }

    // WORKFLOW ACTIONS
    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Submit(string id)
    {
        var (success, reason) = await _svc.SubmitAsync(id, User.Identity?.Name ?? "hr");
        if (!success) TempData["Err"] = reason ?? "Gửi duyệt thất bại";
        else TempData["Msg"] = "Đã gửi duyệt câu hỏi";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Approve)]
    public async Task<IActionResult> Approve(string id)
    {
        var (success, reason) = await _svc.ApproveAsync(id, User.Identity?.Name ?? "hr");
        if (!success) TempData["Err"] = reason ?? "Phê duyệt thất bại";
        else TempData["Msg"] = "Đã phê duyệt câu hỏi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Approve)]
    public async Task<IActionResult> Reject(string id, string? reason)
    {
        var (success, r) = await _svc.RejectAsync(id, User.Identity?.Name ?? "hr", reason);
        if (!success) TempData["Err"] = r ?? "Từ chối thất bại";
        else TempData["Msg"] = "Đã từ chối câu hỏi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreVersion(string id, int auditId)
    {
        var (success, reason) = await _svc.RestoreVersionAsync(id, auditId, User.Identity?.Name ?? "hr");
        if (!success) TempData["Err"] = reason ?? "Khôi phục phiên bản thất bại";
        else TempData["Msg"] = "Đã khôi phục phiên bản câu hỏi từ audit log";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // DELETE 1 MEDIA
    [HttpPost]
    public async Task<IActionResult> DeleteMedia(string questionId, string mediaId)
    {
        var result = await _svc.RemoveMediaAsync(questionId, mediaId, User.Identity?.Name ?? "hr");
        if (!result.Success)
        {
            TempData["Err"] = result.Reason ?? "Không thể xóa file";
            return RedirectToAction(nameof(Edit), new { id = questionId });
        }

        if (!string.IsNullOrWhiteSpace(result.MediaUrl))
        {
            try
            {
                var physical = Path.Combine(_env.WebRootPath, result.MediaUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }
            catch { /* ignore */ }
        }

        return RedirectToAction(nameof(Edit), new { id = questionId });
    }

    private async Task<List<MediaFile>> SaveMediaAsync(List<IFormFile> files)
    {
        var result = new List<MediaFile>();
        var root = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(root);

        var allowed = _cfg.GetSection("AllowedUploadContentTypes").Get<string[]>() ?? new[]
        {
            "image/jpeg","image/png","image/gif","image/webp",
            "audio/mpeg","audio/wav","audio/ogg",
            "video/mp4",
            "application/pdf"
        };
        var maxBytes = _cfg.GetValue<long?>("MaxUploadFileSizeBytes") ?? 10L * 1024 * 1024;

        foreach (var f in files)
        {
            if (f.Length == 0) continue;
            if (f.Length > maxBytes)
                throw new InvalidOperationException($"File {f.FileName} vượt quá {(maxBytes / (1024 * 1024))}MB.");

            var contentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();

            bool okType =
                allowed.Contains(contentType)
                || (allowed.Any(a => a.StartsWith("image/")) && (ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp"))
                || (allowed.Contains("application/pdf") && ext == ".pdf")
                || (allowed.Any(a => a.StartsWith("audio/")) && (ext is ".mp3" or ".wav" or ".ogg"))
                || (allowed.Any(a => a.StartsWith("video/")) && (ext is ".mp4"));

            if (!okType)
                throw new InvalidOperationException($"Loại file {f.FileName} ({contentType}) không được phép.");

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(root, name);

            using var fs = System.IO.File.Create(path);
            await f.CopyToAsync(fs);

            result.Add(new MediaFile
            {
                FileName = f.FileName,
                Url = $"/uploads/{name}",
                ContentType = contentType,
                Size = f.Length
            });
        }
        return result;
    }
}

