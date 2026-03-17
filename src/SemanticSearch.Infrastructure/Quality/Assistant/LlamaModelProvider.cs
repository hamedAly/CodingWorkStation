using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace SemanticSearch.Infrastructure.Quality.Assistant;

public sealed class LlamaModelProvider : IAiAssistantModelProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemanticSearchOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly AssistantReadinessService _readinessService;
    private readonly ILogger<LlamaModelProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private LLamaWeights? _weights;
    private ModelParams? _modelParams;

    public LlamaModelProvider(
        IWebHostEnvironment environment,
        IOptions<SemanticSearchOptions> options,
        AssistantReadinessService readinessService,
        ILogger<LlamaModelProvider> logger,
        ILoggerFactory loggerFactory)
    {
        _environment = environment;
        _options = options.Value;
        _readinessService = readinessService;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public AssistantStatusModel GetStatus()
        => _readinessService.GetStatus();

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_weights is not null && _modelParams is not null)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_weights is not null && _modelParams is not null)
            {
                return;
            }

            var modelPath = ResolveModelPath();
            var modelLabel = Path.GetFileNameWithoutExtension(modelPath);
            _readinessService.MarkInitializing(modelLabel);

            if (!File.Exists(modelPath))
            {
                HandleInitializationFailure(modelLabel, $"The local GGUF model was not found at '{modelPath}'.", null);
                return;
            }

            NativeLibraryConfig.All
                .WithCuda(false)
                .WithVulkan(false)
                .WithAutoFallback(true)
                .WithLogCallback(_logger);

            ConfigureNativeBackend();

            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = (uint)Math.Max(512, _options.Assistant.ContextSize),
                GpuLayerCount = Math.Max(0, _options.Assistant.GpuLayerCount),
                Threads = Math.Max(1, _options.Assistant.CpuThreads),
                BatchThreads = Math.Max(1, _options.Assistant.CpuThreads),
                UseMemorymap = true
            };

            _weights = LLamaWeights.LoadFromFile(modelParams);
            _modelParams = modelParams;
            _readinessService.MarkReady(modelLabel);
            _logger.LogInformation("Loaded local assistant model {ModelLabel} from {ModelPath}.", modelLabel, modelPath);
        }
        catch (Exception ex)
        {
            HandleInitializationFailure(Path.GetFileNameWithoutExtension(ResolveModelPath()), "The local assistant model could not be initialized.", ex);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async ValueTask<IAiAssistantExecutor> CreateExecutorAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_weights is null || _modelParams is null)
        {
            throw new ServiceUnavailableException(GetStatus().FailureReason ?? "The local assistant is unavailable.");
        }

        var contextLogger = _loggerFactory.CreateLogger<LLamaContext>();
        var executorLogger = _loggerFactory.CreateLogger<InteractiveExecutor>();
        var context = _weights.CreateContext(_modelParams, contextLogger);
        var executor = new InteractiveExecutor(context, executorLogger);
        return new LlamaAssistantExecutor(context, executor);
    }

    public async ValueTask DisposeAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            _weights?.Dispose();
            _weights = null;
            _modelParams = null;
        }
        finally
        {
            _initializationLock.Release();
            _initializationLock.Dispose();
        }
    }

    private string ResolveModelPath()
        => Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.Assistant.ModelPath));

    private void ConfigureNativeBackend()
    {
        var nativeDirectory = ResolveNativeLibraryDirectory();
        if (nativeDirectory is null)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            WindowsNativeLibrary.SetDllDirectory(nativeDirectory);
        }

        var llamaPath = Path.Combine(nativeDirectory, GetNativeLibraryName("llama"));
        var mtmdPath = Path.Combine(nativeDirectory, GetNativeLibraryName("mtmd"));
        if (File.Exists(llamaPath) && File.Exists(mtmdPath))
        {
            NativeLibraryConfig.All.WithLibrary(llamaPath, mtmdPath);
            _logger.LogInformation("Configured LLamaSharp native backend from {NativeDirectory}.", nativeDirectory);
            return;
        }

        NativeLibraryConfig.All.WithSearchDirectory(AppContext.BaseDirectory);
        _logger.LogInformation("Configured LLamaSharp native search directory to {BaseDirectory}.", AppContext.BaseDirectory);
    }

    private static string? ResolveNativeLibraryDirectory()
    {
        var runtimeId = ResolveRuntimeId();
        if (runtimeId is null)
        {
            return null;
        }

        var nativeRoot = Path.Combine(AppContext.BaseDirectory, "runtimes", runtimeId, "native");
        if (!Directory.Exists(nativeRoot))
        {
            return null;
        }

        foreach (var candidate in GetNativeLibraryCandidates(nativeRoot))
        {
            if (File.Exists(Path.Combine(candidate, GetNativeLibraryName("llama"))))
            {
                return candidate;
            }
        }

        return nativeRoot;
    }

    private static IEnumerable<string> GetNativeLibraryCandidates(string nativeRoot)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            yield return Path.Combine(nativeRoot, "avx2");
            yield return Path.Combine(nativeRoot, "avx");
            yield return Path.Combine(nativeRoot, "noavx");
        }

        yield return nativeRoot;
    }

    private static string? ResolveRuntimeId()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            return null;
        }

        if (OperatingSystem.IsWindows())
        {
            return "win-x64";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux-x64";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx-x64";
        }

        return null;
    }

    private static string GetNativeLibraryName(string libraryName)
    {
        if (OperatingSystem.IsWindows())
        {
            return $"{libraryName}.dll";
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"lib{libraryName}.dylib";
        }

        return $"lib{libraryName}.so";
    }

    private void HandleInitializationFailure(string modelLabel, string message, Exception? exception)
    {
        var failureMessage = BuildFailureMessage(message, exception);

        if (exception is null)
        {
            _logger.LogWarning("{Message}", message);
            _readinessService.MarkUnavailable(modelLabel, failureMessage);
        }
        else
        {
            _logger.LogError(exception, "{Message}", message);
            _readinessService.MarkFailed(modelLabel, failureMessage);
        }

        _weights?.Dispose();
        _weights = null;
        _modelParams = null;

        if (_options.Assistant.StartupMode == AssistantStartupMode.FailFast)
        {
            throw new ServiceUnavailableException(failureMessage);
        }
    }

    private static string BuildFailureMessage(string message, Exception? exception)
    {
        if (exception is null)
        {
            return message;
        }

        var details = exception.GetBaseException().Message;
        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        return $"{message} {details}";
    }

    private static class WindowsNativeLibrary
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetDllDirectory(string lpPathName);
    }

    private sealed class LlamaAssistantExecutor : IAiAssistantExecutor
    {
        private readonly LLamaContext _context;
        private readonly InteractiveExecutor _executor;

        public LlamaAssistantExecutor(LLamaContext context, InteractiveExecutor executor)
        {
            _context = context;
            _executor = executor;
        }

        public async IAsyncEnumerable<string> InferAsync(
            AssistantPromptModel prompt,
            AssistantInferenceOptionsModel options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var inferenceParams = new InferenceParams
            {
                MaxTokens = options.MaxTokens,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = options.Temperature
                },
                AntiPrompts = options.AntiPrompts
                    .Where(promptStop => !string.IsNullOrWhiteSpace(promptStop))
                    .ToList()
            };

            var renderedPrompt = RenderChatMlPrompt(prompt);
            await foreach (var token in _executor.InferAsync(renderedPrompt, inferenceParams, cancellationToken))
            {
                yield return token;
            }
        }

        private static string RenderChatMlPrompt(AssistantPromptModel prompt)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(prompt.SystemPrompt))
            {
                builder.AppendLine("<|im_start|>system");
                builder.AppendLine(prompt.SystemPrompt.Trim());
                builder.AppendLine("<|im_end|>");
            }

            builder.AppendLine("<|im_start|>user");
            builder.AppendLine(prompt.UserPrompt.Trim());
            builder.AppendLine("<|im_end|>");
            builder.AppendLine("<|im_start|>assistant");
            return builder.ToString();
        }

        public ValueTask DisposeAsync()
        {
            _context.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
