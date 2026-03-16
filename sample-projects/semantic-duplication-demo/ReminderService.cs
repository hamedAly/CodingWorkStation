namespace SemanticDuplicationDemo;

public sealed class ReminderService
{
    public string BuildReminder(string accountId, DateOnly dueDate)
    {
        return $"Reminder for {accountId}: payment is due on {dueDate:yyyy-MM-dd}.";
    }
}
