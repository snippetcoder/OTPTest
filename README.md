# OTP Test Testing Guide

The program is written using .NET Core Razor Pages codebase.

## Prerequisites To Test

- Install Visual Studio 2022 (Community is fine) with "ASP.NET and web development"
- Install .NET 8 SDK (if Visual Studio didn't include it)

## Open & Run

1. Open `OTPTest.sln` in Visual Studio
2. Set startup project to `OTPTest.Api` (right-click → Set as Startup Project)
3. Trust HTTPS dev cert if prompted (Allow/Yes)
4. Press F5 (Start Debugging). The app launches and opens a browser (look for a URL like `https://localhost:5001`)

## Test Via Browser UI (recommended)

<img width="1541" height="522" alt="image" src="https://github.com/user-attachments/assets/025e5726-06b1-4406-8103-a849f9211575" />

### Request OTP
- Go to `https://localhost:5001/Otp/Request`
- Enter an email ending with `dso.org.sg` (e.g., `tester@dept.dso.org.sg`) and click "Send OTP"
- You'll see a success message and a 60-second countdown

### Get the OTP code
- In Visual Studio, open the "Debug" output window
- Look for a line that includes the 6-digit code (the app logs the OTP during development)

### Verify OTP
- Go to `https://localhost:5001/Otp/Verify?email=<the same email>`
- Enter the 6-digit code and click "Verify"
- Expect "OTP verified. You are signed in." if correct and within 1 minute

## Test Via API (optional)

### Using Swagger
- Navigate to `https://localhost:5001/swagger`
- Try `POST /api/otp/generate` with:
  ```json
  { "email": "tester@dept.dso.org.sg" }
  ```
- Check Visual Studio Output for the OTP code
- Then `POST /api/otp/verify` with:
  ```json
  { "email": "tester@dept.dso.org.sg", "code": "<the code>" }
  ```

## Expected Responses

### Generate
- **200 OK**: `{ "status": "STATUS_EMAIL_OK" }`
- **400 BadRequest**: `{ "status": "STATUS_EMAIL_INVALID" }` (bad domain/format)
- **500**: `{ "status": "STATUS_EMAIL_FAIL" }` (email sending failure)

### Verify
- **200 OK**: `{ "status": "STATUS_OTP_OK" }`
- **400 BadRequest**: `{ "status": "STATUS_OTP_FAIL" }` (wrong/locked)
- **400 BadRequest**: `{ "status": "STATUS_OTP_TIMEOUT" }` (expired/no active OTP)

## Happy-Path Test

1. Generate OTP for `user@dept.dso.org.sg`
2. Copy the 6-digit code from the app console log
3. Verify within 1 minute → expect `STATUS_OTP_OK`

## Negative Tests

- **Invalid domain**: Generate with `user@gmail.com` → expect `STATUS_EMAIL_INVALID`
- **Wrong code**: Verify with wrong code → expect `STATUS_OTP_FAIL` and attempt count increments
- **Lockout**: Submit 10 wrong codes → expect `STATUS_OTP_FAIL` and OTP status becomes Locked
- **Timeout**: Wait > 60 seconds after generate, then verify → expect `STATUS_OTP_TIMEOUT`
- **Regenerate**: Call generate again for the same email; old active OTP is expired automatically; only the latest is valid

## Database

- Currently DB setup is **EnsureCreated()**
- Current connection string: `Data Source=otp.db`
- When you run via `dotnet run` in `OTPTest.Api`, the file is created in `OTPTest.Api/otp.db`

### How to view DB?

1. Install "DB Browser for SQLite" (free)
2. File → Open Database → select `otp.db`:
   - If you run from the project folder: `OTPTest.Api/otp.db`
   - If you run from Visual Studio: `OTPTest.Api/bin/Debug/net8.0/otp.db`
3. Browse Data → table `OtpEntries`
4. Run SQL:
   ```sql
   SELECT Id, Email, AttemptCount, MaxAttempts, Status, CreatedAtUtc, ExpiresAtUtc 
   FROM OtpEntries 
   ORDER BY CreatedAtUtc DESC;
   ```
