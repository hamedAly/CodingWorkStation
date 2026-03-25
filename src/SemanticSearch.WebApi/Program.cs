using FluentValidation;
using Hangfire;
using Hangfire.InMemory;
using MediatR;
using Microsoft.AspNetCore.Components;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Behaviors;
using SemanticSearch.Infrastructure.BackgroundJobs;
using SemanticSearch.Infrastructure.DependencyInjection;
using SemanticSearch.Infrastructure.VectorStore;
using SemanticSearch.WebApi.Components;
using SemanticSearch.WebApi.Middleware;
using SemanticSearch.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SemanticSearchOptions>(
    builder.Configuration.GetSection(SemanticSearchOptions.SectionName));

builder.Services.Configure<IntegrationOptions>(
    builder.Configuration.GetSection($"{SemanticSearchOptions.SectionName}:Integration"));

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
builder.Services.AddSingleton<MarkdownRenderService>();
builder.Services.AddScoped<AiStreamEventWriter>();

builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage(new InMemoryStorageOptions()));
builder.Services.AddHangfireServer();

builder.Services.AddHttpClient("TfsClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var ignoreTls = builder.Configuration
            .GetValue<bool>($"{SemanticSearchOptions.SectionName}:Integration:IgnoreTlsErrors");
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ignoreTls
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null
        };
    });
builder.Services.AddHttpClient("SlackClient", client =>
{
    client.BaseAddress = new Uri("https://slack.com/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("AladhanClient", client =>
{
    client.BaseAddress = new Uri("https://api.aladhan.com/");
});

var app = builder.Build();

await app.Services.GetRequiredService<SqliteVectorStore>().InitializeAsync();
await app.Services.GetRequiredService<SemanticSearch.Application.Common.Interfaces.IAiAssistantModelProvider>()
    .EnsureInitializedAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

BackgroundJobRegistration.RegisterAll(app.Services);

app.Run();
