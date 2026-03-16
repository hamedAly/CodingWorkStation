using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Application.Quality;

public sealed class QualityRefreshPolicy
{
    public bool ShouldRefresh(QualitySummarySnapshot? summary, IReadOnlyList<IndexedFile> files)
    {
        if (summary is null)
            return true;

        return files.Any(file => file.LastIndexedUtc > summary.LastAnalyzedUtc);
    }
}
