Overview

- Tech stack: ASP.NET Core Web API (.NET 8), Entity Framework Core (Code First, SQLite), Swagger for API docs.
- Module: Email OTP module with clear controller/service/model separation.
- Endpoints: `POST /api/otp/generate` and `POST /api/otp/verify`.

Assumptions

- Only email addresses ending with `.dso.org.sg` are allowed to receive OTPs.
- OTP lifetime is 1 minute; max 10 verification attempts.
- Email body format is exactly: `You OTP Code is 123456. The code is valid for 1 minute`.
- A real email sender can be plugged in via `IEmailSender`. Current default is a debug sender that logs the email content (for development/testing). Replace with SMTP/SES/etc. in production.
- EF Core uses SQLite (`otp.db` file) and calls `EnsureCreated` on startup for simplicity.

Projects/Files

- `OTPTest.Api/Program.cs` — composition root (DI, DbContext, Swagger).
- `OTPTest.Api/Data/ApplicationDbContext.cs` — EF Core context.
- `OTPTest.Api/Models/OtpEntry.cs` — OTP entity (hashed code + salt, expiry, attempts, status).
- `OTPTest.Api/Services/IEmailOtpModule.cs` — module contract and status enums.
- `OTPTest.Api/Services/EmailOtpModule.cs` — implementation for generate/check logic.
- `OTPTest.Api/Services/IEmailSender.cs` — email abstraction.
- `OTPTest.Api/Services/DebugEmailSender.cs` — logs emails instead of sending.
- `OTPTest.Api/Controllers/OtpController.cs` — REST endpoints.

Build & Run

1. Install .NET 8 SDK.
2. Restore and run:
   - `cd OTPTest.Api`
   - `dotnet restore`
   - `dotnet run`
3. Open Swagger at `https://localhost:5001/swagger` (or console URL) to try endpoints.

API Usage

- Generate OTP
  - `POST /api/otp/generate`
  - Body: `{ "email": "user@xyz.dso.org.sg" }`
  - Responses: `STATUS_EMAIL_OK`, `STATUS_EMAIL_INVALID`, `STATUS_EMAIL_FAIL`.

- Verify OTP
  - `POST /api/otp/verify`
  - Body: `{ "email": "user@xyz.dso.org.sg", "code": "123456" }`
  - Responses: `STATUS_OTP_OK`, `STATUS_OTP_FAIL`, `STATUS_OTP_TIMEOUT`.

Testing Strategy

- Unit tests (service level):
  - Email validation: rejects non-`.dso.org.sg` and malformed emails.
  - OTP generation: creates an active entry with 1-min TTL and 0 attempts.
  - Email send outcomes: simulate success/failure via a fake `IEmailSender`.
  - Verification: success on correct code; attempt increments on wrong code; lock at 10 attempts; timeout on expired.
  - Security: stored code is hashed with salt; never persists plain OTP.

- Integration tests (API level):
  - Use `WebApplicationFactory` with `InMemory` EF provider.
  - End-to-end generate + verify happy path.
  - Expiry path by overriding system clock or manipulating entity `ExpiresAtUtc`.

- Manual verification:
  - Run service, call `generate`, inspect console logs for OTP.
  - Call `verify` within a minute; should return `STATUS_OTP_OK` for correct code.

Replacing Email Sender

- Implement `IEmailSender` using `System.Net.Mail.SmtpClient` or a 3rd-party SDK and register it in DI instead of `DebugEmailSender` in `Program.cs`.

Notes on Pseudocode Mapping

- `start()`/`close()` map to `Start()`/`Close()` in `IEmailOtpModule` and are no-ops.
- `generate_OTP_email(...)` maps to `GenerateOtpEmailAsync(...)`.
- `check_OTP(...)` maps to `CheckOtpAsync(...)`. The REST endpoint uses single-submit verification rather than interactive stream, but the module supports the interactive pattern via a delegate.

