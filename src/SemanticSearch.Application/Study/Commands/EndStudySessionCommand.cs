using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record EndStudySessionCommand(string SessionId) : IRequest<StudySessionModel>;

public sealed class EndStudySessionCommandHandler : IRequestHandler<EndStudySessionCommand, StudySessionModel>
{
    private readonly IStudyRepository _studyRepository;

    public EndStudySessionCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<StudySessionModel> Handle(EndStudySessionCommand request, CancellationToken cancellationToken)
    {
        var existingSession = await _studyRepository.GetSessionByIdAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException($"Study session '{request.SessionId}' was not found.");

        var endedAt = DateTime.UtcNow;
        var durationMinutes = Math.Max(1, (int)Math.Round((endedAt - existingSession.StartedAt).TotalMinutes, MidpointRounding.AwayFromZero));
        await _studyRepository.UpdateSessionEndAsync(request.SessionId, endedAt, durationMinutes, cancellationToken);

        return new StudySessionModel(existingSession.Id, existingSession.BookId, existingSession.ChapterId, existingSession.SessionType, existingSession.StartedAt, endedAt, durationMinutes, existingSession.IsPomodoroSession, existingSession.FocusDurationMinutes);
    }
}