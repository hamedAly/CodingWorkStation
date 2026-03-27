using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record StartStudySessionCommand(string SessionType, string? BookId, string? ChapterId, bool IsPomodoro, int? FocusDurationMinutes) : IRequest<StudySessionModel>;

public sealed class StartStudySessionCommandHandler : IRequestHandler<StartStudySessionCommand, StudySessionModel>
{
    private readonly IStudyRepository _studyRepository;

    public StartStudySessionCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<StudySessionModel> Handle(StartStudySessionCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.BookId))
        {
            _ = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
                ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.ChapterId))
        {
            _ = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
                ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");
        }

        var session = new StudySession
        {
            Id = Guid.NewGuid().ToString("N"),
            BookId = string.IsNullOrWhiteSpace(request.BookId) ? null : request.BookId,
            ChapterId = string.IsNullOrWhiteSpace(request.ChapterId) ? null : request.ChapterId,
            SessionType = request.SessionType,
            StartedAt = DateTime.UtcNow,
            IsPomodoroSession = request.IsPomodoro,
            FocusDurationMinutes = request.FocusDurationMinutes,
            CreatedAt = DateTime.UtcNow
        };

        await _studyRepository.InsertSessionAsync(session, cancellationToken);
        return session.ToModel();
    }
}