using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Components;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Behaviors;
using SemanticSearch.Infrastructure.DependencyInjection;
using SemanticSearch.Infrastructure.VectorStore;
using SemanticSearch.WebApi.Components;
using SemanticSearch.WebApi.Middleware;
using SemanticSearch.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SemanticSearchOptions>(
    builder.Configuration.GetSection(SemanticSearchOptions.SectionName));

var semanticSearchOptions = builder.Configuration
    .GetSection(SemanticSearchOptions.SectionName)
    .Get<SemanticSearchOptions>() ?? new SemanticSearchOptions();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.RegisterServicesFromAssembly(typeof(SemanticSearch.Application.Common.SemanticSearchOptions).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(SemanticSearch.Application.Common.SemanticSearchOptions).Assembly);
builder.Services.AddInfrastructure(builder.Environment, semanticSearchOptions);
builder.Services.AddControllers();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<WorkspaceApiClient>();

var app = builder.Build();

await app.Services.GetRequiredService<SqliteVectorStore>().InitializeAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
