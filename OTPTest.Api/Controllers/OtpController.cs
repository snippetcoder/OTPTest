using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OTPTest.Api.Data;
using OTPTest.Api.Models;
using OTPTest.Api.Models.Dto;
using OTPTest.Api.Services;

namespace OTPTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OtpController : ControllerBase
{
    private readonly IEmailOtpModule _otpModule;
    private readonly ApplicationDbContext _db;

    public OtpController(IEmailOtpModule otpModule, ApplicationDbContext db)
    {
        _otpModule = otpModule;
        _db = db;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateOtpRequest request, CancellationToken ct)
    {
        var status = await _otpModule.GenerateOtpEmailAsync(request.Email, ct);
        return status switch
        {
            EmailStatus.STATUS_EMAIL_OK => Ok(new { status = nameof(EmailStatus.STATUS_EMAIL_OK) }),
            EmailStatus.STATUS_EMAIL_INVALID => BadRequest(new { status = nameof(EmailStatus.STATUS_EMAIL_INVALID) }),
            _ => StatusCode(500, new { status = nameof(EmailStatus.STATUS_EMAIL_FAIL) })
        };
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        // API style: single submission (not interactive stream). Reuse module verification semantics.
        var now = DateTime.UtcNow;
        var entry = await _db.OtpEntries
            .Where(e => e.Email == request.Email && e.Status == OtpStatus.Active)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
        {
            return BadRequest(new { status = nameof(OtpStatusCode.STATUS_OTP_TIMEOUT), reason = "No active OTP" });
        }

        if (now > entry.ExpiresAtUtc)
        {
            entry.Status = OtpStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return BadRequest(new { status = nameof(OtpStatusCode.STATUS_OTP_TIMEOUT), reason = "OTP expired" });
        }

        // Verify code using same logic as module
        var verified = EmailOtpModule_VerifyProxy(request.Code, entry.Salt, entry.CodeHash);
        if (verified)
        {
            entry.Status = OtpStatus.Verified;
            await _db.SaveChangesAsync(ct);
            return Ok(new { status = nameof(OtpStatusCode.STATUS_OTP_OK) });
        }

        entry.AttemptCount++;
        if (entry.AttemptCount >= entry.MaxAttempts)
        {
            entry.Status = OtpStatus.Locked;
        }
        await _db.SaveChangesAsync(ct);
        return BadRequest(new { status = nameof(OtpStatusCode.STATUS_OTP_FAIL) });
    }

    // Small proxy to reuse static verification logic without exposing internals publicly.
    private static bool EmailOtpModule_VerifyProxy(string code, string salt, string hash)
    {
        return (bool)typeof(EmailOtpModule)
            .GetMethod("VerifyCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { code, salt, hash })!;
    }
}

