using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdateChapterNotesCommand(string BookId, string ChapterId, string? Notes) : IRequest;

public sealed class UpdateChapterNotesCommandHandler : IRequestHandler<UpdateChapterNotesCommand>
{
    private readonly IStudyRepository _studyRepository;

    public UpdateChapterNotesCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task Handle(UpdateChapterNotesCommand request, CancellationToken cancellationToken)
    {
        _ = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
        var chapter = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");

        var updated = new StudyChapter
        {
            Id = chapter.Id,
            BookId = chapter.BookId,
            Title = chapter.Title,
            StartPage = chapter.StartPage,
            EndPage = chapter.EndPage,
            SortOrder = chapter.SortOrder,
            AudioFileName = chapter.AudioFileName,
            AudioFilePath = chapter.AudioFilePath,
            Notes = request.Notes,
            CreatedAt = chapter.CreatedAt
        };

        await _studyRepository.UpdateChapterAsync(updated, cancellationToken);
    }
}
