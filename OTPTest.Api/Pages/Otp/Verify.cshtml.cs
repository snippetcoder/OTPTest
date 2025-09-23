using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OTPTest.Api.Data;
using OTPTest.Api.Models;
using OTPTest.Api.Services;

namespace OTPTest.Api.Pages.Otp;

public class VerifyModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public VerifyModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public void OnGet([FromQuery] string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email!;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Code))
        {
            IsError = true;
            Message = "Email and 6-digit code are required.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var entry = await _db.OtpEntries
            .Where(e => e.Email == Email && e.Status == OtpStatus.Active)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
        {
            IsError = true;
            Message = "No active OTP. Please request a new code.";
            return Page();
        }

        if (now > entry.ExpiresAtUtc)
        {
            entry.Status = OtpStatus.Expired;
            await _db.SaveChangesAsync(ct);
            IsError = true;
            Message = "OTP expired. Please request a new code.";
            return Page();
        }

        // Verify using EmailOtpModule's private static helper via reflection (to avoid duplicating hashing logic)
        var ok = (bool)typeof(EmailOtpModule)
            .GetMethod("VerifyCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { Code, entry.Salt, entry.CodeHash })!;

        if (ok)
        {
            entry.Status = OtpStatus.Verified;
            await _db.SaveChangesAsync(ct);
            IsError = false;
            Message = "OTP verified. You are signed in.";
            return Page();
        }

        entry.AttemptCount++;
        if (entry.AttemptCount >= entry.MaxAttempts)
        {
            entry.Status = OtpStatus.Locked;
        }
        await _db.SaveChangesAsync(ct);
        IsError = true;
        Message = "Incorrect code. Please try again.";
        return Page();
    }
}

