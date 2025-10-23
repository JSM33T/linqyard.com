using Linkyard.Repositories;
using Linqyard.Api.Extensions; // Custom CORS
using Linqyard.Api.Middleware; // CorrelationIdMiddleware
using Linqyard.Api.Services;
using Linqyard.Contracts.Interfaces;
using Linqyard.Infra;
using Linqyard.Infra.Configuration;
using Linqyard.Repositories;
using Linqyard.Repositories.Configuration;
using Linqyard.Services;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Bootstrap Serilog very early ---
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // read from appsettings.json
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .Enrich.WithProperty("Application", "Linqyard.Api")
    .CreateLogger();

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithExceptionDetails()
       .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName);
});

// --- 2. Register services ---
builder.Services.AddControllers();
builder.Services.AddOpenApi(); // built-in OpenAPI/Swagger

// --- Entity Framework Core with PostgreSQL ---
builder.Services.AddLinqyardDbContext(builder.Configuration, builder.Environment);

// --- JWT Configuration ---
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);

// --- CORS Configuration ---
builder.Services.AddCustomCors();

// --- Rate limiting ---
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddScoped<IRateLimiterService, RateLimiterService>();

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHttpClient();

// Email Service Configuration
builder.Services.Configure<Linqyard.Infra.Configuration.SmtpSettings>(
    builder.Configuration.GetSection("Email:Smtp"));
builder.Services.AddScoped<Linqyard.Infra.IEmailService, Linqyard.Infra.EmailService>();

// Azure Blob Storage Service Configuration
builder.Services.Configure<Linqyard.Infra.Configuration.AzureBlobStorageSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.AddScoped<Linqyard.Infra.IAzureBlobStorageService, Linqyard.Infra.AzureBlobStorageService>();

// Tier & payment configuration
builder.Services.Configure<RazorpaySettings>(builder.Configuration.GetSection("Payments:Razorpay"));

// Repository Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<ILinkService, LinkService>();
builder.Services.AddScoped<IViewTelemetryRepository, ViewTelemetryRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITierRepository, TierRepository>();
builder.Services.AddScoped<ITierService, TierService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Add custom app services (example)
// builder.Services.AddSingleton<ILoggingService, LoggingService>();

var app = builder.Build();

// --- 3. Configure middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Serilog request logging (logs HTTP requests with correlation info)
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RequestHost", http.Request.Host.Value!);
        diag.Set("RequestScheme", http.Request.Scheme);
        diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString()!);
        diag.Set("UserId", http.User?.FindFirst("oid")?.Value ?? http.User?.Identity?.Name!);
        diag.Set("CorrelationId", http.Request.Headers["X-Correlation-Id"].ToString());
    };
});
// Correlation ID middleware (ensures every request has one)
app.UseMiddleware<CorrelationIdMiddleware>();

// Enable CORS
app.UseCustomCors();

// Only enforce HTTPS redirection in non-development environments. In local dev
// it's common to run the API over HTTP which would otherwise cause a 307
// redirect when the frontend calls the API. Keep HTTPS enforced in prod.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- 4. Start app with safe logging ---
try
{
    Log.Information("Starting Linqyard.Api...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Linqyard.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
