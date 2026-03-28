using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdateChapterCommand(string BookId, string ChapterId, string Title, int StartPage, int EndPage) : IRequest<ChapterModel>;

public sealed class UpdateChapterCommandHandler : IRequestHandler<UpdateChapterCommand, ChapterModel>
{
    private readonly IStudyRepository _studyRepository;

    public UpdateChapterCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<ChapterModel> Handle(UpdateChapterCommand request, CancellationToken cancellationToken)
    {
        var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
        var chapter = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");

        if (request.StartPage < 1 || request.StartPage > book.PageCount || request.EndPage < request.StartPage || request.EndPage > book.PageCount)
            throw new InvalidOperationException($"Chapter page range must be within 1 and {book.PageCount}.");

        var updated = new StudyChapter
        {
            Id = chapter.Id,
            BookId = chapter.BookId,
            Title = request.Title.Trim(),
            StartPage = request.StartPage,
            EndPage = request.EndPage,
            SortOrder = chapter.SortOrder,
            AudioFileName = chapter.AudioFileName,
            AudioFilePath = chapter.AudioFilePath,
            Notes = chapter.Notes,
            CreatedAt = chapter.CreatedAt
        };

        await _studyRepository.UpdateChapterAsync(updated, cancellationToken);
        return updated.ToModel();
    }
}
