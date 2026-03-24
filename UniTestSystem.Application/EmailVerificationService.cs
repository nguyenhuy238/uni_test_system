using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public class EmailVerificationService
    {
        private const string EntityName = "EmailVerification";
        private const string ActionIssued = "Issued";
        private const string ActionVerified = "Verified";

        private readonly IRepository<AuditEntry> _auditRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IEmailSender _emailSender;

        public EmailVerificationService(
            IRepository<AuditEntry> auditRepo,
            IRepository<User> userRepo,
            IEmailSender emailSender)
        {
            _auditRepo = auditRepo;
            _userRepo = userRepo;
            _emailSender = emailSender;
        }

        public async Task IssueVerificationEmailAsync(string userId, string userEmail, string userName, string baseUrl)
        {
            var token = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
            var expiresAtUtc = DateTime.UtcNow.AddHours(24);

            var payload = new VerificationPayload
            {
                Token = token,
                Email = userEmail,
                ExpiresAtUtc = expiresAtUtc
            };

            await _auditRepo.InsertAsync(new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = userId,
                Action = ActionIssued,
                EntityName = EntityName,
                EntityId = userId,
                After = JsonSerializer.Serialize(payload)
            });

            var root = (baseUrl ?? "").Trim().TrimEnd('/');
            var verifyUrl = $"{root}/auth/confirm-email?token={Uri.EscapeDataString(token)}";

            var html = $@"
<h3>Xác nhận email tài khoản UniTestSystem</h3>
<p>Xin chào <b>{System.Net.WebUtility.HtmlEncode(userName)}</b>,</p>
<p>Vui lòng bấm vào liên kết bên dưới để xác nhận email:</p>
<p><a href=""{verifyUrl}"">{verifyUrl}</a></p>
<p>Liên kết có hiệu lực đến: <b>{expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC</b>.</p>
<p>Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này.</p>";

            await _emailSender.SendAsync(userEmail, "[UniTestSystem] Xác nhận email đăng ký", html);
        }

        public async Task<(bool Success, string Message)> ConfirmEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Liên kết xác nhận không hợp lệ.");

            var issuedEntries = await _auditRepo.Query()
                .Where(x => x.EntityName == EntityName && x.Action == ActionIssued)
                .OrderByDescending(x => x.At)
                .ToListAsync();

            AuditEntry? issued = null;
            VerificationPayload? issuedPayload = null;

            foreach (var entry in issuedEntries)
            {
                var payload = DeserializePayload(entry.After);
                if (payload == null) continue;
                if (!string.Equals(payload.Token, token, StringComparison.Ordinal)) continue;

                issued = entry;
                issuedPayload = payload;
                break;
            }

            if (issued == null || issuedPayload == null)
                return (false, "Liên kết xác nhận không tồn tại hoặc đã bị thu hồi.");

            if (issuedPayload.ExpiresAtUtc < DateTime.UtcNow)
                return (false, "Liên kết xác nhận đã hết hạn.");

            var verifyEntries = await _auditRepo.Query()
                .Where(x => x.EntityName == EntityName && x.Action == ActionVerified && x.EntityId == issued.EntityId)
                .ToListAsync();

            foreach (var entry in verifyEntries)
            {
                var payload = DeserializePayload(entry.After);
                if (payload != null && string.Equals(payload.Token, token, StringComparison.Ordinal))
                    return (true, "Email đã được xác nhận trước đó.");
            }

            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == issued.EntityId);
            if (user == null)
                return (false, "Không tìm thấy người dùng cho liên kết xác nhận.");

            await _auditRepo.InsertAsync(new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = user.Id,
                Action = ActionVerified,
                EntityName = EntityName,
                EntityId = user.Id,
                After = JsonSerializer.Serialize(new VerificationPayload
                {
                    Token = token,
                    Email = user.Email,
                    ExpiresAtUtc = issuedPayload.ExpiresAtUtc
                })
            });

            return (true, "Xác nhận email thành công.");
        }

        private static VerificationPayload? DeserializePayload(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<VerificationPayload>(json);
            }
            catch
            {
                return null;
            }
        }

        private sealed class VerificationPayload
        {
            public string Token { get; set; } = "";
            public string Email { get; set; } = "";
            public DateTime ExpiresAtUtc { get; set; }
        }
    }
}
