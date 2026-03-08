using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Org_Manage)]
[Route("api/admin/enrollments")]
public class AdminEnrollmentsController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public AdminEnrollmentsController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet("{courseId}")]
    public async Task<IActionResult> GetByCourse(string courseId)
    {
        var list = await _academicService.GetEnrollmentsByCourseAsync(courseId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
    {
        var success = await _academicService.EnrollStudentAsync(request.StudentId, request.CourseId, request.Semester);
        return success ? Ok() : BadRequest();
    }

    [HttpDelete("{studentId}/{courseId}")]
    public async Task<IActionResult> Unenroll(string studentId, string courseId)
    {
        var success = await _academicService.UnenrollStudentAsync(studentId, courseId);
        return success ? NoContent() : NotFound();
    }
}

public class EnrollmentRequest
{
    public string StudentId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string Semester { get; set; } = "";
}
