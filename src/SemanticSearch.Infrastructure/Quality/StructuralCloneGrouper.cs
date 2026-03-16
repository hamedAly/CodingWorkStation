using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.Quality;

public sealed record StructuralCandidate(string Fingerprint, int MatchingLineCount, DetectedCodeRegion Region);

public sealed class StructuralCloneGrouper
{
    public IReadOnlyList<DetectedCodeClone> BuildFindings(
        IReadOnlyList<StructuralCandidate> candidates,
        int maxFindings)
    {
        var findings = new List<DetectedCodeClone>();

        foreach (var group in candidates.GroupBy(candidate => candidate.Fingerprint).Where(group => group.Count() > 1))
        {
            var items = group.OrderByDescending(candidate => candidate.MatchingLineCount).ToArray();
            for (var leftIndex = 0; leftIndex < items.Length - 1; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < items.Length; rightIndex++)
                {
                    var left = items[leftIndex];
                    var right = items[rightIndex];
                    if (left.Region == right.Region)
                    {
                        continue;
                    }

                    findings.Add(new DetectedCodeClone(
                        DuplicationType.Structural,
                        1d,
                        Math.Min(left.MatchingLineCount, right.MatchingLineCount),
                        left.Region,
                        right.Region,
                        group.Key));
                }
            }
        }

        return findings
            .OrderByDescending(finding => finding.MatchingLineCount)
            .ThenBy(finding => finding.Left.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Left.StartLine)
            .Take(maxFindings)
            .ToList();
    }
}

