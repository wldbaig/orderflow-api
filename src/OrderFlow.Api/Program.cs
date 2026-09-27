using Microsoft.OpenApi.Models;
using OrderFlow.Api.Common;
using OrderFlow.Api.Startup;
using OrderFlow.Application;
using OrderFlow.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Structured logging (Serilog) ---
builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Layers ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Web ---
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderFlow API",
        Version = "v1",
        Description = "B2B order-intake API — clean business logic, performant reads, and AI woven into the intake flow."
    });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

var app = builder.Build();

// Global exception handling first, so nothing downstream leaks an unformatted error.
app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "OrderFlow API");
}

app.UseHttpsRedirection();
app.MapControllers();

// Migrate + seed before serving traffic (configurable).
await DatabaseInitializer.InitializeAsync(app);

app.Run();

// Exposed so integration tests can reference the entry point.
public partial class Program { }
