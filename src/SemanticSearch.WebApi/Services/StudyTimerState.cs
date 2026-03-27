using SemanticSearch.WebApi.Contracts.Study;

namespace SemanticSearch.WebApi.Services;

public sealed class StudyTimerState
{
    private DateTime _timerStartedAtUtc;
    private int _remainingSecondsWhenPaused;

    public StudySessionResponse? ActiveSession { get; private set; }
    public string SessionType { get; private set; } = string.Empty;
    public string? BookId { get; private set; }
    public string? ChapterId { get; private set; }
    public int TotalSeconds { get; private set; }
    public int BreakDurationMinutes { get; private set; } = 5;
    public bool IsPaused { get; private set; }

    public bool HasActiveSession => ActiveSession is not null;

    public void Start(StudySessionResponse session, int totalSeconds, int breakDurationMinutes, string sessionType, string? bookId, string? chapterId)
    {
        ActiveSession = session;
        SessionType = sessionType;
        BookId = bookId;
        ChapterId = chapterId;
        TotalSeconds = Math.Max(totalSeconds, 1);
        BreakDurationMinutes = Math.Max(breakDurationMinutes, 1);
        _timerStartedAtUtc = DateTime.UtcNow;
        _remainingSecondsWhenPaused = TotalSeconds;
        IsPaused = false;
    }

    public int GetRemainingSeconds()
    {
        if (!HasActiveSession)
            return 0;

        if (IsPaused)
            return _remainingSecondsWhenPaused;

        var elapsedSeconds = (int)Math.Floor((DateTime.UtcNow - _timerStartedAtUtc).TotalSeconds);
        return Math.Max(0, TotalSeconds - elapsedSeconds);
    }

    public void Pause(int remainingSeconds)
    {
        if (!HasActiveSession)
            return;

        _remainingSecondsWhenPaused = Math.Max(0, remainingSeconds);
        IsPaused = true;
    }

    public void Resume()
    {
        if (!HasActiveSession || !IsPaused)
            return;

        _timerStartedAtUtc = DateTime.UtcNow.AddSeconds(-Math.Max(0, TotalSeconds - _remainingSecondsWhenPaused));
        IsPaused = false;
    }

    public void Clear()
    {
        ActiveSession = null;
        SessionType = string.Empty;
        BookId = null;
        ChapterId = null;
        TotalSeconds = 0;
        BreakDurationMinutes = 5;
        _remainingSecondsWhenPaused = 0;
        IsPaused = false;
    }
}