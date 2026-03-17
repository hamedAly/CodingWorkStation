namespace SemanticSearch.Application.Common;

public sealed class SemanticSearchOptions
{
    public const string SectionName = "SemanticSearch";

    public string ModelPath { get; set; } = "models/all-MiniLM-L6-v2";
    public string DatabasePath { get; set; } = "data/vectorstore.db";
    public ChunkingOptions Chunking { get; set; } = new();
    public IndexingOptions Indexing { get; set; } = new();
    public UiOptions Ui { get; set; } = new();
    public QualityOptions Quality { get; set; } = new();
    public AssistantOptions Assistant { get; set; } = new();
}

public sealed class ChunkingOptions
{
    public int ChunkSize { get; set; } = 200;
    public int Overlap { get; set; } = 40;
}

public sealed class IndexingOptions
{
    public string[] ExcludedDirectories { get; set; } =
    [
        "bin", "obj", ".git", ".vs", ".venv", "node_modules", "packages",
        "dist", "build", "target", "coverage", "out", "debug", "release"
    ];

    public string[] AllowedExtensions { get; set; } =
    [
        ".cs", ".ts", ".tsx", ".js", ".jsx",
        ".py", ".rb", ".go", ".rs", ".java",
        ".cpp", ".c", ".h", ".hpp",
        ".md", ".txt", ".json", ".yaml", ".yml",
        ".xml", ".html", ".css", ".sql", ".razor"
    ];
}

public sealed class UiOptions
{
    public int DashboardPollSeconds { get; set; } = 5;
    public int DefaultSemanticTopK { get; set; } = 5;
    public int DefaultExactTopK { get; set; } = 50;
}

public sealed class QualityOptions
{
    public int MinimumStructuralLines { get; set; } = 5;
    public double SemanticSimilarityThreshold { get; set; } = 0.9;
    public int MaxSemanticPairs { get; set; } = 500;
    public int MaxFindingsPerType { get; set; } = 100;
}

public sealed class AssistantOptions
{
    public string ModelPath { get; set; } = "models/llm/qwen2.5-coder-instruct.gguf";
    public int ContextSize { get; set; } = 8192;
    public int MaxTokens { get; set; } = 768;
    public int CpuThreads { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
    public int GpuLayerCount { get; set; }
    public string[] AntiPrompts { get; set; } = ["User:", "System:"];
    public int MaxDuplicateSnippetCharacters { get; set; } = 12000;
    public float Temperature { get; set; } = 0.2f;
    public AssistantStartupMode StartupMode { get; set; } = AssistantStartupMode.MarkUnavailable;
}

public enum AssistantStartupMode
{
    FailFast = 0,
    MarkUnavailable = 1
}
