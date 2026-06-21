using Microsoft.EntityFrameworkCore;
using PRM.Api.Extensions;
using PRM.Api.Filters;
using PRM.Api.Middleware;
using PRM.Api.Services;
using PRM.Application;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Scheduling;
using PRM.Infrastructure;
using PRM.Infrastructure.Email;
using PRM.Persistence;
using PRM.Persistence.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console());

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddPrmSwagger();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPrmHangfire(builder.Configuration, builder.Environment);
builder.Services.AddPrmHangfireServer(builder.Environment);
builder.Services.AddPrmAuthentication(builder.Configuration);

var app = builder.Build();

var emailSection = app.Configuration.GetSection(EmailOptions.SectionName);
var smtpUsername = emailSection.GetSection("Smtp")["Username"];
app.Logger.LogInformation(
    "Email config loaded: Provider={Provider}, Enabled={Enabled}, FromEmail={FromEmail}, SmtpUser={SmtpUser}",
    emailSection["Provider"],
    emailSection["Enabled"],
    emailSection["FromEmail"],
    smtpUsername);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");

    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex,
            "Failed to apply database migrations. If you changed the database name, ensure the database is empty " +
            "or drop any partially-migrated database and restart. Connection: {Database}",
            db.Database.GetDbConnection().Database);
        throw;
    }

    await DatabaseSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());

    if (!app.Environment.IsEnvironment("Testing"))
    {
        var config = await scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>().GetAsync();
        var jobScheduleManager = scope.ServiceProvider.GetRequiredService<IPrmJobScheduleManager>();
        jobScheduleManager.ScheduleRecurringJob(config.SchedulerIntervalMinutes);
        jobScheduleManager.EnqueueImmediateRun();
    }
}

app.UsePrmSwagger();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UsePrmHangfireDashboard();
app.UseMiddleware<RequiresPasswordChangeMiddleware>();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .WithName("HealthLive")
    .WithTags("Health")
    .WithSummary("Liveness probe — returns OK if the API process is running.");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
    .WithName("HealthReady")
    .WithTags("Health")
    .WithSummary("Readiness probe — returns OK if the API is ready to accept traffic.");

app.Run();

public partial class Program;
