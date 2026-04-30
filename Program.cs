using AUTH_Sevice.Extensions;
using AUTH_Sevice.Infrastructure.Data;
using AUTH_Sevice.Middlewares;
using Serilog;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// ─── Serilog ───────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ──────────────────────────────────────────────────────────────
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddApiRateLimiting()
    .AddSwaggerDocumentation()
    .AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// ─── App pipeline ──────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup (dev/staging only)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AUTH-Service v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (ctx, httpContext) =>
    {
        ctx.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        ctx.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting AuthService...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuthService failed to start.");
}
finally
{
    Log.CloseAndFlush();
}