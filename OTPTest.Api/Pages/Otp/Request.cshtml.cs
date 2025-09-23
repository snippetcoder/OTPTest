using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OTPTest.Api.Services;

namespace OTPTest.Api.Pages.Otp;

public class RequestModel : PageModel
{
    private readonly IEmailOtpModule _otp;

    public RequestModel(IEmailOtpModule otp)
    {
        _otp = otp;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? Message { get; set; }
    public bool IsError { get; set; }
    public bool Sent { get; set; }
    public int SecondsLeft { get; set; } = 60;

    public void OnGet([FromQuery] string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email!;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            IsError = true;
            Message = "Email is required.";
            return Page();
        }

        var status = await _otp.GenerateOtpEmailAsync(Email, ct);
        switch (status)
        {
            case EmailStatus.STATUS_EMAIL_OK:
                Sent = true;
                IsError = false;
                Message = $"OTP sent to {Email}. It is valid for 1 minute.";
                SecondsLeft = 60;
                break;
            case EmailStatus.STATUS_EMAIL_INVALID:
                IsError = true;
                Message = "Invalid email. Use your dso.org.sg email.";
                break;
            default:
                IsError = true;
                Message = "Failed to send OTP. Please try again.";
                break;
        }

        return Page();
    }
}

