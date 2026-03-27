namespace UniTestSystem.Configuration;

public sealed class SecurityRateLimitingOptions
{
    public SecurityRateLimitPolicyOptions Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = 0 };
    public SecurityRateLimitPolicyOptions Forgot { get; set; } = new() { PermitLimit = 3, WindowSeconds = 900, QueueLimit = 0 };
    public SecurityRateLimitPolicyOptions Reset { get; set; } = new() { PermitLimit = 5, WindowSeconds = 900, QueueLimit = 0 };
}

public sealed class SecurityRateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int QueueLimit { get; set; }
}
