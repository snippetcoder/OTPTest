using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OTPTest.Api.Data;
using OTPTest.Api.Models;

namespace OTPTest.Api.Services;

public class EmailOtpModule : IEmailOtpModule
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailOtpModule> _logger;

    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(1);
    private const int MaxTries = 10;
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public EmailOtpModule(ApplicationDbContext db, IEmailSender emailSender, ILogger<EmailOtpModule> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public void Start()
    {
        // no-op for now
    }

    public void Close()
    {
        // no-op for now
    }

    public async Task<EmailStatus> GenerateOtpEmailAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(userEmail))
        {
            return EmailStatus.STATUS_EMAIL_INVALID;
        }

        if (!IsAllowedDomain(userEmail))
        {
            return EmailStatus.STATUS_EMAIL_INVALID;
        }

        // Expire any existing active OTPs for this email
        var now = DateTime.UtcNow;
        var existing = await _db.OtpEntries
            .Where(e => e.Email == userEmail && e.Status == OtpStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var e in existing)
        {
            e.Status = OtpStatus.Expired;
        }

        var code = GenerateSixDigitCode();
        var (salt, hash) = HashCode(code);

        var entry = new OtpEntry
        {
            Email = userEmail,
            Salt = salt,
            CodeHash = hash,
            AttemptCount = 0,
            MaxAttempts = MaxTries,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(OtpTtl),
            Status = OtpStatus.Active
        };

        _db.OtpEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        var body = $"You OTP Code is {code}. The code is valid for 1 minute";
        var ok = await _emailSender.SendEmailAsync(userEmail, "Your OTP Code", body, cancellationToken);

        return ok ? EmailStatus.STATUS_EMAIL_OK : EmailStatus.STATUS_EMAIL_FAIL;
    }

    public async Task<OtpStatusCode> CheckOtpAsync(Func<CancellationToken, Task<string>> readOtp, string email, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OtpTtl);

        for (var attempt = 0; attempt < MaxTries; attempt++)
        {
            string code;
            try
            {
                code = await readOtp(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return OtpStatusCode.STATUS_OTP_TIMEOUT;
            }

            var now = DateTime.UtcNow;
            var entry = await _db.OtpEntries
                .Where(e => e.Email == email && e.Status == OtpStatus.Active)
                .OrderByDescending(e => e.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry is null)
            {
                return OtpStatusCode.STATUS_OTP_TIMEOUT; // No active OTP found (or expired/used)
            }

            if (now > entry.ExpiresAtUtc)
            {
                entry.Status = OtpStatus.Expired;
                await _db.SaveChangesAsync(cancellationToken);
                return OtpStatusCode.STATUS_OTP_TIMEOUT;
            }

            if (VerifyCode(code, entry.Salt, entry.CodeHash))
            {
                entry.Status = OtpStatus.Verified;
                await _db.SaveChangesAsync(cancellationToken);
                return OtpStatusCode.STATUS_OTP_OK;
            }

            entry.AttemptCount++;
            if (entry.AttemptCount >= entry.MaxAttempts)
            {
                entry.Status = OtpStatus.Locked;
                await _db.SaveChangesAsync(cancellationToken);
                return OtpStatusCode.STATUS_OTP_FAIL;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return OtpStatusCode.STATUS_OTP_FAIL;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex.IsMatch(email);
    }

    private static bool IsAllowedDomain(string email)
    {
        // Only allow dso.org.sg or its subdomains
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return false;
        var domain = email[(at + 1)..];
        return domain.Equals("dso.org.sg", StringComparison.OrdinalIgnoreCase)
               || domain.EndsWith(".dso.org.sg", StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateSixDigitCode()
    {
        var n = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return n.ToString("D6");
    }

    private static (string salt, string hash) HashCode(string code)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToHexString(saltBytes);
        using var sha = SHA256.Create();
        var input = Encoding.UTF8.GetBytes(code + ":" + salt);
        var hashBytes = sha.ComputeHash(input);
        var hash = Convert.ToHexString(hashBytes);
        return (salt, hash);
    }

    private static bool VerifyCode(string code, string salt, string expectedHash)
    {
        using var sha = SHA256.Create();
        var input = Encoding.UTF8.GetBytes(code + ":" + salt);
        var actual = Convert.ToHexString(sha.ComputeHash(input));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(actual),
            Encoding.ASCII.GetBytes(expectedHash));
    }
}
