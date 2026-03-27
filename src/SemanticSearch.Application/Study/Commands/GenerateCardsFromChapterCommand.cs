using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record GenerateCardsFromChapterCommand(string DeckId, string ChapterId) : IRequest<GeneratedCardsModel>;

public sealed class GenerateCardsFromChapterCommandHandler : IRequestHandler<GenerateCardsFromChapterCommand, GeneratedCardsModel>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly IAiAssistantModelProvider _modelProvider;

    public GenerateCardsFromChapterCommandHandler(
        IFlashCardRepository flashCardRepository,
        IStudyRepository studyRepository,
        IAiAssistantModelProvider modelProvider)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
        _modelProvider = modelProvider;
    }

    public async Task<GeneratedCardsModel> Handle(GenerateCardsFromChapterCommand request, CancellationToken cancellationToken)
    {
        var deck = await _flashCardRepository.GetDeckByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard deck '{request.DeckId}' was not found.");
        var chapter = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");

        if (string.IsNullOrWhiteSpace(chapter.Notes))
            throw new ValidationException([new ValidationFailure(nameof(request.ChapterId), "Chapter notes are required to generate flashcards.")]);

        if (!string.IsNullOrWhiteSpace(deck.BookId) && !string.Equals(deck.BookId, chapter.BookId, StringComparison.Ordinal))
            throw new ValidationException([new ValidationFailure(nameof(request.ChapterId), "The selected chapter must belong to the deck's book.")]);

        var book = await _studyRepository.GetBookByIdAsync(chapter.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{chapter.BookId}' was not found.");

        await _modelProvider.EnsureInitializedAsync(cancellationToken);
        var status = _modelProvider.GetStatus();
        if (!string.Equals(status.Status, "Ready", StringComparison.OrdinalIgnoreCase))
            throw new ServiceUnavailableException(status.FailureReason ?? "The local assistant is unavailable.");

        var prompt = BuildPrompt(chapter.Title, book.Title, chapter.Notes);
        var rawResponse = new StringBuilder();

        await using (var executor = await _modelProvider.CreateExecutorAsync(cancellationToken))
        {
            await foreach (var token in executor.InferAsync(prompt, BuildInferenceOptions(), cancellationToken))
            {
                rawResponse.Append(token);
            }
        }

        var generated = ParseResponse(rawResponse.ToString());
        if (generated.Count == 0)
            throw new ValidationException([new ValidationFailure(nameof(request.ChapterId), "The local assistant did not return any flashcards.")]);

        var now = DateTime.UtcNow;
        var cards = generated.Select(item => new FlashCard
        {
            Id = Guid.NewGuid().ToString("N"),
            DeckId = deck.Id,
            ChapterId = chapter.Id,
            Front = item.Question.Trim(),
            Back = item.Answer.Trim(),
            Interval = 0,
            Repetitions = 0,
            EaseFactor = 2.5d,
            NextReviewDate = now.Date,
            CreatedAt = now
        }).ToList();

        await _flashCardRepository.InsertCardsAsync(cards, cancellationToken);
        return new GeneratedCardsModel(cards.Select(card => card.ToModel()).ToList(), cards.Count);
    }

    private static AssistantPromptModel BuildPrompt(string chapterTitle, string bookTitle, string notes)
        => new(
            "You are an expert educator. Given study notes, create flashcard question-answer pairs. Return a JSON array of objects with \"question\" and \"answer\" fields. Create concise, focused cards that test understanding, not just memorization. Output ONLY valid JSON, no markdown formatting.",
            $"Create flashcards from these chapter notes:{Environment.NewLine}Title: {chapterTitle}{Environment.NewLine}Book: {bookTitle}{Environment.NewLine}Notes:{Environment.NewLine}{notes.Trim()}{Environment.NewLine}{Environment.NewLine}Generate between 3 and 10 flashcards depending on content density.");

    private static AssistantInferenceOptionsModel BuildInferenceOptions()
        => new(
            900,
            0.2f,
            ["<|im_end|>", "<|endoftext|>", "<|im_start|>user", "<|im_start|>system"]);

    private static List<GeneratedFlashCard> ParseResponse(string response)
    {
        var trimmed = response.Trim();
        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
            trimmed = trimmed[arrayStart..(arrayEnd + 1)];

        try
        {
            var cards = JsonSerializer.Deserialize<List<GeneratedFlashCard>>(trimmed, JsonOptions);
            return cards?
                .Where(card => !string.IsNullOrWhiteSpace(card.Question) && !string.IsNullOrWhiteSpace(card.Answer))
                .Take(10)
                .ToList() ?? [];
        }
        catch (JsonException ex)
        {
            throw new ValidationException([new ValidationFailure("response", $"The local assistant returned invalid flashcard JSON. {ex.Message}")]);
        }
    }

    private sealed record GeneratedFlashCard(string Question, string Answer);
}