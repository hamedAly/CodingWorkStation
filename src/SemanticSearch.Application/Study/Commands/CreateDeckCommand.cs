using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record CreateDeckCommand(string Title, string? BookId) : IRequest<DeckDetailModel>;

public sealed class CreateDeckCommandHandler : IRequestHandler<CreateDeckCommand, DeckDetailModel>
{
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;

    public CreateDeckCommandHandler(IFlashCardRepository flashCardRepository, IStudyRepository studyRepository)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
    }

    public async Task<DeckDetailModel> Handle(CreateDeckCommand request, CancellationToken cancellationToken)
    {
        string? bookTitle = null;
        if (!string.IsNullOrWhiteSpace(request.BookId))
        {
            var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
                ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
            bookTitle = book.Title;
        }

        var now = DateTime.UtcNow;
        var deck = new FlashCardDeck
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            BookId = string.IsNullOrWhiteSpace(request.BookId) ? null : request.BookId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _flashCardRepository.InsertDeckAsync(deck, cancellationToken);
        return new DeckDetailModel(deck.Id, deck.Title, deck.BookId, bookTitle, [], 0);
    }
}