using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Behaviors;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.DependencyInjection;
using SemanticSearch.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration
builder.Services.Configure<SemanticSearchOptions>(
    builder.Configuration.GetSection(SemanticSearchOptions.SectionName));

var semanticSearchOptions = builder.Configuration
    .GetSection(SemanticSearchOptions.SectionName)
    .Get<SemanticSearchOptions>() ?? new SemanticSearchOptions();

// Register MediatR with pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.RegisterServicesFromAssembly(typeof(SemanticSearch.Application.Common.SemanticSearchOptions).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Register FluentValidation validators via assembly scanning
builder.Services.AddValidatorsFromAssembly(typeof(SemanticSearch.Application.Common.SemanticSearchOptions).Assembly);

// Register Infrastructure services
builder.Services.AddInfrastructure(builder.Environment, semanticSearchOptions);

// Add controllers
builder.Services.AddControllers();

// Startup validation: verify model files exist
var modelDir = Path.Combine(builder.Environment.ContentRootPath, semanticSearchOptions.ModelPath);
var modelFile = Path.Combine(modelDir, "model.onnx");
var vocabFile = Path.Combine(modelDir, "vocab.txt");

if (!File.Exists(modelFile) || !File.Exists(vocabFile))
{
    var logger = LoggerFactory.Create(x => x.AddConsole()).CreateLogger("Startup");
    logger.LogError(
        "ONNX model files not found. Expected model.onnx and vocab.txt in '{ModelDir}'. " +
        "Download from HuggingFace: sentence-transformers/all-MiniLM-L6-v2",
        modelDir);
}

var app = builder.Build();

// Initialize vector store schema
var vectorStore = app.Services.GetRequiredService<IVectorStore>();
await vectorStore.InitializeAsync();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.MapControllers();

app.Run();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
