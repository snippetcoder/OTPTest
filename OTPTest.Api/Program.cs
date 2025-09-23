using Microsoft.EntityFrameworkCore;
using OTPTest.Api.Data;
using OTPTest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core DbContext (SQLite file by default)
var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=otp.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Email sending + OTP services
builder.Services.AddScoped<IEmailSender, DebugEmailSender>();
builder.Services.AddScoped<IEmailOtpModule, EmailOtpModule>();

var app = builder.Build();

// Ensure database exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.UseStaticFiles();
app.MapRazorPages();

app.Run();

// Expose Program for WebApplicationFactory integration testing
public partial class Program { }
