namespace SemanticSearch.Infrastructure.Tfs;

public static class TfsQueryBuilder
{
    public static string BuildAssignedWorkItemsWiql(string? projectFilter = null)
    {
        var projectClause = projectFilter is not null
            ? $" AND [System.TeamProject] = '{projectFilter}'"
            : " AND [System.TeamProject] <> ''";
        return $"SELECT [System.Id] FROM WorkItems WHERE [System.AssignedTo] = @Me{projectClause} AND [System.State] <> 'Removed' ORDER BY [System.ChangedDate] DESC";
    }

    public static string BuildContributionWiql(string since)
        => $"SELECT [System.Id], [System.ChangedDate] FROM WorkItems WHERE [System.ChangedBy] = @Me AND [System.ChangedDate] >= '{since}' ORDER BY [System.ChangedDate] DESC";
}
