namespace OTPTest.Api.Models;

public enum OtpStatus
{
    Active = 0,
    Verified = 1,
    Expired = 2,
    Locked = 3
}

public class OtpEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 10;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public OtpStatus Status { get; set; } = OtpStatus.Active;
}

