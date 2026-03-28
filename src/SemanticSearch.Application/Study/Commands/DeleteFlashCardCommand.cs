using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record DeleteFlashCardCommand(string DeckId, string CardId) : IRequest;

public sealed class DeleteFlashCardCommandHandler : IRequestHandler<DeleteFlashCardCommand>
{
    private readonly IFlashCardRepository _flashCardRepository;

    public DeleteFlashCardCommandHandler(IFlashCardRepository flashCardRepository)
    {
        _flashCardRepository = flashCardRepository;
    }

    public async Task Handle(DeleteFlashCardCommand request, CancellationToken cancellationToken)
    {
        var existingCard = await _flashCardRepository.GetCardByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard '{request.CardId}' was not found.");

        if (!string.Equals(existingCard.DeckId, request.DeckId, StringComparison.Ordinal))
            throw new NotFoundException($"Flashcard '{request.CardId}' was not found in deck '{request.DeckId}'.");

        await _flashCardRepository.DeleteCardAsync(request.CardId, cancellationToken);
    }
}