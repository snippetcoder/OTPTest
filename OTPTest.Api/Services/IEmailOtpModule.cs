using System.IO;

namespace OTPTest.Api.Services;

public enum EmailStatus
{
    STATUS_EMAIL_OK,
    STATUS_EMAIL_FAIL,
    STATUS_EMAIL_INVALID
}

public enum OtpStatusCode
{
    STATUS_OTP_OK,
    STATUS_OTP_FAIL,
    STATUS_OTP_TIMEOUT
}

public interface IEmailOtpModule
{
    void Start();
    void Close();

    Task<EmailStatus> GenerateOtpEmailAsync(string userEmail, CancellationToken cancellationToken = default);

    // For interactive environments that block on input stream. Not used by HTTP directly.
    Task<OtpStatusCode> CheckOtpAsync(Func<CancellationToken, Task<string>> readOtp, string email, CancellationToken cancellationToken = default);
}

