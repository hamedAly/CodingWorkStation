using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdateBookCommand(string BookId, string Title, string? Author, string? Description) : IRequest<BookDetailModel>;

public sealed class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, BookDetailModel>
{
    private readonly IStudyRepository _studyRepository;

    public UpdateBookCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<BookDetailModel> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var existingBook = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");

        var updatedBook = new StudyBook
        {
            Id = existingBook.Id,
            Title = request.Title.Trim(),
            Author = request.Author?.Trim(),
            Description = request.Description?.Trim(),
            FileName = existingBook.FileName,
            FilePath = existingBook.FilePath,
            PageCount = existingBook.PageCount,
            LastReadPage = existingBook.LastReadPage,
            CreatedAt = existingBook.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _studyRepository.UpdateBookAsync(updatedBook, cancellationToken);
        var chapters = await _studyRepository.GetChaptersByBookIdAsync(updatedBook.Id, cancellationToken);
        return updatedBook.ToModel(chapters);
    }
}
