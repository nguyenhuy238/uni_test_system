using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace UniTestSystem.Application
{
    public class ExamAccessTokenService
    {
        private readonly byte[] _secretKey;

        public ExamAccessTokenService(IConfiguration configuration)
        {
            var secret = configuration["ExamAccessToken:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = configuration["Jwt:Key"];
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("ExamAccessToken secret is missing.");
            }

            _secretKey = Encoding.UTF8.GetBytes(secret);
        }

        public string Generate(string userId, string testId, string scheduleId, DateTime expiresAtUtc)
        {
            var expUnix = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
            var payload = $"{userId}|{testId}|{scheduleId}|{expUnix}";
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var signature = ComputeSignature(payloadBase64);
            return $"{payloadBase64}.{signature}";
        }

        public bool Validate(string token, string userId, string testId, string scheduleId, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(token))
            {
                error = "Missing access token.";
                return false;
            }

            var parts = token.Split('.', 2);
            if (parts.Length != 2)
            {
                error = "Malformed access token.";
                return false;
            }

            var payloadBase64 = parts[0];
            var receivedSignature = parts[1];
            var expectedSignature = ComputeSignature(payloadBase64);

            if (!FixedTimeEquals(receivedSignature, expectedSignature))
            {
                error = "Invalid token signature.";
                return false;
            }

            string payload;
            try
            {
                payload = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64));
            }
            catch
            {
                error = "Invalid token payload.";
                return false;
            }

            var fields = payload.Split('|');
            if (fields.Length != 4)
            {
                error = "Invalid token structure.";
                return false;
            }

            var tokenUserId = fields[0];
            var tokenTestId = fields[1];
            var tokenScheduleId = fields[2];
            if (!long.TryParse(fields[3], out var expUnix))
            {
                error = "Invalid token expiry.";
                return false;
            }

            if (!string.Equals(tokenUserId, userId, StringComparison.Ordinal))
            {
                error = "Token user mismatch.";
                return false;
            }

            if (!string.Equals(tokenTestId, testId, StringComparison.Ordinal))
            {
                error = "Token test mismatch.";
                return false;
            }

            if (!string.Equals(tokenScheduleId, scheduleId, StringComparison.Ordinal))
            {
                error = "Token schedule mismatch.";
                return false;
            }

            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            if (DateTime.UtcNow > expiresAtUtc)
            {
                error = "Access token expired.";
                return false;
            }

            return true;
        }

        private string ComputeSignature(string payloadBase64)
        {
            using var hmac = new HMACSHA256(_secretKey);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            return Base64UrlEncode(bytes);
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var l = Encoding.UTF8.GetBytes(left);
            var r = Encoding.UTF8.GetBytes(right);
            return CryptographicOperations.FixedTimeEquals(l, r);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var padded = input.Replace('-', '+').Replace('_', '/');
            var pad = 4 - (padded.Length % 4);
            if (pad is > 0 and < 4)
            {
                padded = padded.PadRight(padded.Length + pad, '=');
            }

            return Convert.FromBase64String(padded);
        }
    }
}
