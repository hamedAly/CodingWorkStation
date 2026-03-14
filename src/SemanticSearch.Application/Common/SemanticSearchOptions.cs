namespace SemanticSearch.Application.Common;

public sealed class SemanticSearchOptions
{
    public const string SectionName = "SemanticSearch";

    public string ModelPath { get; set; } = "models/all-MiniLM-L6-v2";
    public string DatabasePath { get; set; } = "data/vectorstore.db";
    public ChunkingOptions Chunking { get; set; } = new();
    public IndexingOptions Indexing { get; set; } = new();
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
        "bin", "obj", ".git", "node_modules", ".vs", "packages"
    ];

    public string[] AllowedExtensions { get; set; } =
    [
        ".cs", ".ts", ".tsx", ".js", ".jsx",
        ".py", ".rb", ".go", ".rs", ".java",
        ".cpp", ".c", ".h", ".hpp",
        ".md", ".txt", ".json", ".yaml", ".yml",
        ".xml", ".html", ".css", ".sql"
    ];
}
