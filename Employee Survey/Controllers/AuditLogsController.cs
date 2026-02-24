using Employee_Survey.Application;
using Employee_Survey.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers
{
    [Authorize]
    public class AuditLogsController : Controller
    {
        private readonly IAuditReaderService _reader;
        private readonly IPermissionService _perms;

        public AuditLogsController(IAuditReaderService reader, IPermissionService perms)
        { _reader = reader; _perms = perms; }

        [HttpGet("/audit")]
        public async Task<IActionResult> Index(string? from = null, string? to = null, string? keyword = null, string? actor = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Audit_View))
                return Redirect("/auth/denied");

            DateTime? fromUtc = null, toUtc = null;
            if (DateTime.TryParse(from, out var f)) fromUtc = DateTime.SpecifyKind(f, DateTimeKind.Utc);
            if (DateTime.TryParse(to, out var t)) toUtc = DateTime.SpecifyKind(t, DateTimeKind.Utc);

            var list = await _reader.GetAllAsync(fromUtc, toUtc, keyword, actor);
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Keyword = keyword;
            ViewBag.Actor = actor;
            return View("Index", list);
        }
    }
}
