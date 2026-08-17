using PgQueue.Abstractions;
using PgQueue.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPgQueue(
    options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Default");
        options.WorkerCount = 4;
        options.PollingFallbackInterval = TimeSpan.FromSeconds(5);
    });

// Register test handler
builder.Services.AddJobHandler<TestJobHandler, TestJobPayload>("test-job");

var app = builder.Build();

app.Run();