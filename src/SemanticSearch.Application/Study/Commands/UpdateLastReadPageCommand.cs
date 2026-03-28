using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdateLastReadPageCommand(string BookId, int Page) : IRequest;

public sealed class UpdateLastReadPageCommandHandler : IRequestHandler<UpdateLastReadPageCommand>
{
    private readonly IStudyRepository _studyRepository;

    public UpdateLastReadPageCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task Handle(UpdateLastReadPageCommand request, CancellationToken cancellationToken)
    {
        var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");

        if (request.Page < 1 || request.Page > book.PageCount)
            throw new InvalidOperationException($"Page must be between 1 and {book.PageCount}.");

        await _studyRepository.UpdateLastReadPageAsync(request.BookId, request.Page, cancellationToken);
    }
}
