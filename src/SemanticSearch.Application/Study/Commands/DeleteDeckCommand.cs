using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record DeleteDeckCommand(string DeckId) : IRequest;

public sealed class DeleteDeckCommandHandler : IRequestHandler<DeleteDeckCommand>
{
    private readonly IFlashCardRepository _flashCardRepository;

    public DeleteDeckCommandHandler(IFlashCardRepository flashCardRepository)
    {
        _flashCardRepository = flashCardRepository;
    }

    public async Task Handle(DeleteDeckCommand request, CancellationToken cancellationToken)
    {
        var existingDeck = await _flashCardRepository.GetDeckByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard deck '{request.DeckId}' was not found.");

        await _flashCardRepository.DeleteDeckAsync(existingDeck.Id, cancellationToken);
    }
}