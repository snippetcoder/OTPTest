namespace OTPTest.Api.Services;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default);
}

