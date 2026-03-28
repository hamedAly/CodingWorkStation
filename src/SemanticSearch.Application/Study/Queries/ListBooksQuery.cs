using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record ListBooksQuery() : IRequest<IReadOnlyList<BookSummaryModel>>;

public sealed class ListBooksQueryHandler : IRequestHandler<ListBooksQuery, IReadOnlyList<BookSummaryModel>>
{
    private readonly IStudyRepository _studyRepository;

    public ListBooksQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<IReadOnlyList<BookSummaryModel>> Handle(ListBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await _studyRepository.GetAllBooksAsync(cancellationToken);
        var results = new List<BookSummaryModel>(books.Count);

        foreach (var book in books)
        {
            var chapterCount = (await _studyRepository.GetChaptersByBookIdAsync(book.Id, cancellationToken)).Count;
            results.Add(new BookSummaryModel(book.Id, book.Title, book.Author, book.PageCount, book.LastReadPage, chapterCount, book.CreatedAt));
        }

        return results;
    }
}
