using Microsoft.Extensions.Logging;

namespace OTPTest.Api.Services;

// Development stub that logs the email body instead of actually sending.
public class DebugEmailSender : IEmailSender
{
    private readonly ILogger<DebugEmailSender> _logger;

    public DebugEmailSender(ILogger<DebugEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Email-> {Email}] {Subject}\n{Body}", emailAddress, subject, body);
        return Task.FromResult(true);
    }
}

