using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record AddChapterCommand(string BookId, string Title, int StartPage, int EndPage) : IRequest<ChapterModel>;

public sealed class AddChapterCommandHandler : IRequestHandler<AddChapterCommand, ChapterModel>
{
    private readonly IStudyRepository _studyRepository;

    public AddChapterCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<ChapterModel> Handle(AddChapterCommand request, CancellationToken cancellationToken)
    {
        var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");

        ValidatePageRange(book.PageCount, request.StartPage, request.EndPage);
        var chapters = await _studyRepository.GetChaptersByBookIdAsync(request.BookId, cancellationToken);
        var chapter = new StudyChapter
        {
            Id = Guid.NewGuid().ToString("N"),
            BookId = request.BookId,
            Title = request.Title.Trim(),
            StartPage = request.StartPage,
            EndPage = request.EndPage,
            SortOrder = chapters.Count,
            CreatedAt = DateTime.UtcNow
        };

        await _studyRepository.InsertChapterAsync(chapter, cancellationToken);
        return chapter.ToModel();
    }

    private static void ValidatePageRange(int pageCount, int startPage, int endPage)
    {
        if (startPage < 1 || startPage > pageCount || endPage < startPage || endPage > pageCount)
            throw new InvalidOperationException($"Chapter page range must be within 1 and {pageCount}.");
    }
}
