namespace UniTestSystem.Application.Interfaces;

public interface ITokenBlacklistService
{
    Task RevokeAsync(string jti, DateTimeOffset expiry);
    Task<bool> IsRevokedAsync(string jti);
}
