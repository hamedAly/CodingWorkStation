using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetBookQuery(string BookId) : IRequest<BookDetailModel>;

public sealed class GetBookQueryHandler : IRequestHandler<GetBookQuery, BookDetailModel>
{
    private readonly IStudyRepository _studyRepository;

    public GetBookQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<BookDetailModel> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
        var chapters = await _studyRepository.GetChaptersByBookIdAsync(request.BookId, cancellationToken);
        return book.ToModel(chapters);
    }
}
