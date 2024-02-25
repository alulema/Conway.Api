using Serilog;
using Serilog.Events;
using Conway.Api.DataAccess;
using Conway.Api.Middleware;
using Conway.Api.Services;
using Conway.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: "logs/error_log_.txt", // Base file name
        rollingInterval: RollingInterval.Day, // Roll over daily
        restrictedToMinimumLevel: LogEventLevel.Error, // Only log error and above
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.Console());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the DbContext with SQLite Database
builder.Services.AddDbContext<GameOfLifeContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("GameOfLifeDb") ?? "Data Source=GameOfLife.db"));

// Register retry policy for resilience
const int retryCount = 3;
var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
builder.Services.AddHttpClient("NamedClient")
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(
        retryCount,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            logger.LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}.");
        }));

builder.Services.AddScoped<IGameOfLifeService, GameOfLifeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();