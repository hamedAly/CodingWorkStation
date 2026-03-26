using SemanticSearch.WebApi.Contracts.Tfs;

namespace SemanticSearch.WebApi.Components.Kanban;

public sealed class KanbanBoardState
{
    public List<WorkItemResponse> Items { get; private set; } = [];
    public bool IsLoading { get; set; }
    public string? Error { get; set; }
    public WorkItemResponse? SelectedItem { get; private set; }
    public WorkItemResponse? DraggedItem { get; private set; }
    public string? DragSourceState { get; private set; }
    public HashSet<int> SyncingItemIds { get; } = [];
    public Dictionary<int, string> ErrorItems { get; } = [];

    public Action? OnStateChanged { get; set; }

    public void Initialize(IEnumerable<WorkItemResponse> items)
    {
        Items = items.ToList();
        NotifyStateChanged();
    }

    public void StartDrag(WorkItemResponse item)
    {
        DraggedItem = item;
        DragSourceState = item.State;
        NotifyStateChanged();
    }

    public void CancelDrag()
    {
        DraggedItem = null;
        DragSourceState = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Optimistically move the dragged item to the target state column.
    /// Returns the item being moved (to trigger async TFS sync), or null if no drag is active.
    /// </summary>
    public WorkItemResponse? DropOnColumn(string targetState)
    {
        if (DraggedItem is null || DraggedItem.State == targetState)
        {
            CancelDrag();
            return null;
        }

        var item = DraggedItem;
        // Optimistic update: replace item with same data but new State
        var updated = item with { State = targetState };
        var idx = Items.IndexOf(item);
        if (idx >= 0) Items[idx] = updated;

        SyncingItemIds.Add(updated.Id);
        DraggedItem = null;
        DragSourceState = null;
        NotifyStateChanged();
        return updated;
    }

    public void CompleteSyncSuccess(int itemId)
    {
        SyncingItemIds.Remove(itemId);
        NotifyStateChanged();
    }

    public void CompleteSyncFailure(int itemId, string error, string originalState)
    {
        SyncingItemIds.Remove(itemId);
        // Revert item to original state
        var idx = Items.FindIndex(i => i.Id == itemId);
        if (idx >= 0)
            Items[idx] = Items[idx] with { State = originalState };
        ErrorItems[itemId] = error;
        NotifyStateChanged();
    }

    public void DismissError(int itemId)
    {
        ErrorItems.Remove(itemId);
        NotifyStateChanged();
    }

    public void SelectItem(WorkItemResponse? item)
    {
        SelectedItem = item;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
