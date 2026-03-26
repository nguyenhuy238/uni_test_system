using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Settings_Edit)]
[Route("api/admin/system-settings")]
public class AdminSystemSettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public AdminSystemSettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _settingsService.GetAsync();
        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSystemSettingsRequest request)
    {
        var current = await _settingsService.GetAsync();
        current.SystemName = string.IsNullOrWhiteSpace(request.SystemName) ? current.SystemName : request.SystemName.Trim();
        current.CurrentSemester = request.CurrentSemester?.Trim();
        current.CurrentAcademicYear = request.CurrentAcademicYear?.Trim();
        current.WarningGpaThreshold = request.WarningGpaThreshold > 0 ? request.WarningGpaThreshold : current.WarningGpaThreshold;
        current.FailGpaThreshold = request.FailGpaThreshold > 0 ? request.FailGpaThreshold : current.FailGpaThreshold;
        if (current.WarningGpaThreshold < current.FailGpaThreshold)
            current.WarningGpaThreshold = current.FailGpaThreshold;
        current.TreatOutstandingDebtAsFail = request.TreatOutstandingDebtAsFail;
        current.LogoUrl = request.LogoUrl?.Trim();
        current.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _settingsService.SaveAsync(current);
        return Ok(current);
    }
}

public class UpdateSystemSettingsRequest
{
    public string? SystemName { get; set; }
    public string? CurrentSemester { get; set; }
    public string? CurrentAcademicYear { get; set; }
    public decimal WarningGpaThreshold { get; set; }
    public decimal FailGpaThreshold { get; set; }
    public bool TreatOutstandingDebtAsFail { get; set; } = true;
    public string? LogoUrl { get; set; }
}
