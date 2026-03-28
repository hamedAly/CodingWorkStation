using MediatR;
using SemanticSearch.Application.Common.Interfaces;

namespace SemanticSearch.Application.Slack.Commands;

public sealed record TriggerJobCommand(string JobName) : IRequest<TriggerJobResult>;

public sealed record TriggerJobResult(bool Queued, string? Error = null);

public sealed class TriggerJobCommandHandler : IRequestHandler<TriggerJobCommand, TriggerJobResult>
{
    private readonly IBackgroundJobDispatcher _dispatcher;

    public TriggerJobCommandHandler(IBackgroundJobDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<TriggerJobResult> Handle(TriggerJobCommand request, CancellationToken cancellationToken)
    {
        switch (request.JobName.ToLowerInvariant())
        {
            case "standup":
                _dispatcher.EnqueueStandup();
                break;
            case "prayer-fetch":
                _dispatcher.EnqueuePrayerFetch();
                break;
            case "study-reminder":
                _dispatcher.EnqueueStudyReminder();
                break;
            default:
                return Task.FromResult(new TriggerJobResult(false, $"Unknown job name: '{request.JobName}'. Valid values: standup, prayer-fetch, study-reminder."));
        }
        return Task.FromResult(new TriggerJobResult(true));
    }
}
