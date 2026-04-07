using FinanceTracker.Api.Services;
using FinanceTracker.API.Adapter;
using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Middleware;
using FinanceTracker.API.Repositories;
using FinanceTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
builder.Services.AddSingleton<IBudgetRepository, BudgetRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<INotificationService, ConsoleNotificationService>();

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();