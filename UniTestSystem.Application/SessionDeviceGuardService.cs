using System.Security.Cryptography;
using System.Text;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public class SessionDeviceGuardService
    {
        private readonly IRepository<Session> _sessionRepo;
        private readonly IRepository<DeviceFingerprint> _deviceRepo;

        public SessionDeviceGuardService(IRepository<Session> sessionRepo, IRepository<DeviceFingerprint> deviceRepo)
        {
            _sessionRepo = sessionRepo;
            _deviceRepo = deviceRepo;
        }

        public string GetRequestFingerprint(string? userAgent, string? ipAddress)
        {
            var normalizedUserAgent = userAgent ?? string.Empty;
            var ip = ipAddress ?? string.Empty;
            var raw = $"ua:{normalizedUserAgent}|ip:{ip}";

            return Sha256Hex(raw);
        }

        public async Task<bool> EnsureSessionDeviceAsync(string sessionId, string requestFingerprint, string? userAgent, string? ipAddress)
        {
            var existing = await _deviceRepo.GetAllAsync(x => x.SessionId == sessionId);
            if (existing.Count == 0)
            {
                await _deviceRepo.InsertAsync(new DeviceFingerprint
                {
                    SessionId = sessionId,
                    Browser = BuildFingerprintTag(requestFingerprint),
                    OS = ExtractOs(userAgent),
                    IP = ipAddress ?? string.Empty,
                    UserAgent = userAgent ?? string.Empty
                });
                return true;
            }

            return existing.Any(df => IsSameFingerprint(df, requestFingerprint));
        }

        public async Task<bool> HasActiveSessionOnOtherDeviceAsync(string userId, string requestFingerprint)
        {
            var activeSessions = await _sessionRepo.GetAllAsync(x => x.UserId == userId && x.Status == SessionStatus.InProgress && !x.IsDeleted);
            if (activeSessions.Count == 0)
            {
                return false;
            }

            foreach (var s in activeSessions)
            {
                var fps = await _deviceRepo.GetAllAsync(x => x.SessionId == s.Id);
                if (fps.Count == 0)
                {
                    continue;
                }

                if (!fps.Any(x => IsSameFingerprint(x, requestFingerprint)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameFingerprint(DeviceFingerprint record, string requestFingerprint)
        {
            if (!string.IsNullOrWhiteSpace(record.Browser) && record.Browser.StartsWith("FP:", StringComparison.Ordinal))
            {
                return string.Equals(record.Browser, BuildFingerprintTag(requestFingerprint), StringComparison.Ordinal);
            }

            var legacyRaw = $"ua:{record.UserAgent}|ip:{record.IP}";
            return string.Equals(Sha256Hex(legacyRaw), requestFingerprint, StringComparison.Ordinal);
        }

        private static string BuildFingerprintTag(string fingerprint) => $"FP:{fingerprint}";

        private static string Sha256Hex(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(data);
            return Convert.ToHexString(hash);
        }

        private static string ExtractOs(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }

            var ua = userAgent.ToLowerInvariant();
            if (ua.Contains("windows")) return "Windows";
            if (ua.Contains("mac os") || ua.Contains("macintosh")) return "MacOS";
            if (ua.Contains("android")) return "Android";
            if (ua.Contains("iphone") || ua.Contains("ios")) return "iOS";
            if (ua.Contains("linux")) return "Linux";
            return "Unknown";
        }
    }
}
