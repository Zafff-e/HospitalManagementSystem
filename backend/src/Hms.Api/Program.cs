using System.Text;
using Hms.Application;
using Hms.Infrastructure;
using Hms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// ─── Serilog Configuration ───
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/hms-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting HMS API application...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ─── Add Layer Services ───
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ─── JWT Authentication ───
    var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "ThisIsAVeryLongSecretKeyForDevelopmentOnly2024!SecureKeyForHms2026";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "hms-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "hms-clients",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // ─── Controllers & Problem Details ───
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path;
        };
    });

    // ─── Health Checks ───
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<HmsDbContext>("database_ready", tags: new[] { "ready" });

    // ─── OpenAPI ───
    builder.Services.AddOpenApi();

    // ─── CORS ───
    const string CorsPolicy = "HmsClientOrigins";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicy, policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200", // Angular Staff Portal
                    "http://localhost:5173", // React Clinical App
                    "http://localhost"       // Nginx Reverse Proxy
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ─── Global Exception Handling ───
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.ContentType = "application/problem+json";
            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerFeature?.Error;

            Log.Error(exception, "Unhandled exception on {Path}", context.Request.Path);

            var statusCode = exception is Hms.Domain.Exceptions.DomainException
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = exception is Hms.Domain.Exceptions.DomainException ? "Business rule violation" : "An error occurred while processing your request.",
                Detail = exception?.Message,
                Instance = context.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problemDetails);
        });
    });

    // ─── Middleware Pipeline ───
    app.UseSerilogRequestLogging();
    app.UseCors(CorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    // OpenAPI & Scalar API Documentation
    app.MapOpenApi("/openapi/v1.json");
    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Hospital Management System API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
        options.OpenApiRoutePattern = "/openapi/v1.json";
    });

    // Health Checks
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Liveness checks if HTTP pipeline is up
    });

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") // Readiness checks DB connectivity
    });

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "HMS API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
