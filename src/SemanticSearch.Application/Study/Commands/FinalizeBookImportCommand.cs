using MediatR;
using FluentValidation;
using FluentValidation.Results;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SemanticSearch.Application.Study.Commands;

public sealed record DetectedChapterInput(string Title, int StartPage, int EndPage);

public sealed record FinalizeBookImportCommand(
    string BookId,
    int PageCount,
    IReadOnlyList<DetectedChapterInput> Chapters,
    string? TableOfContentsText,
    string? PreviewText) : IRequest<BookDetailModel>;

public sealed class FinalizeBookImportCommandHandler : IRequestHandler<FinalizeBookImportCommand, BookDetailModel>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IStudyRepository _studyRepository;
    private readonly IAiAssistantModelProvider _modelProvider;

    public FinalizeBookImportCommandHandler(IStudyRepository studyRepository, IAiAssistantModelProvider modelProvider)
    {
        _studyRepository = studyRepository;
        _modelProvider = modelProvider;
    }

    public async Task<BookDetailModel> Handle(FinalizeBookImportCommand request, CancellationToken cancellationToken)
    {
        var existingBook = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");

        var pageCount = Math.Max(1, request.PageCount);
        var book = await UpdateBookPageCountAsync(existingBook, pageCount, cancellationToken);

        var chapters = await _studyRepository.GetChaptersByBookIdAsync(book.Id, cancellationToken);
        if (chapters.Count == 0)
        {
            chapters = await InsertDetectedChaptersAsync(
                book,
                request.Chapters,
                request.TableOfContentsText,
                request.PreviewText,
                cancellationToken);
        }

        await EnsureAutoPlanAsync(book, chapters, cancellationToken);
        return book.ToModel(chapters);
    }

    private async Task<StudyBook> UpdateBookPageCountAsync(
        StudyBook book,
        int pageCount,
        CancellationToken cancellationToken)
    {
        var lastReadPage = Math.Clamp(book.LastReadPage, 1, pageCount);
        if (book.PageCount == pageCount && book.LastReadPage == lastReadPage)
            return book;

        var updatedBook = new StudyBook
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            FileName = book.FileName,
            FilePath = book.FilePath,
            PageCount = pageCount,
            LastReadPage = lastReadPage,
            CreatedAt = book.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _studyRepository.UpdateBookAsync(updatedBook, cancellationToken);
        return updatedBook;
    }

    private async Task<IReadOnlyList<StudyChapter>> InsertDetectedChaptersAsync(
        StudyBook book,
        IReadOnlyList<DetectedChapterInput> detectedChapters,
        string? tableOfContentsText,
        string? previewText,
        CancellationToken cancellationToken)
    {
        var aiChapters = await DetectChaptersWithAiAsync(
            book.Title,
            book.PageCount,
            detectedChapters,
            tableOfContentsText,
            previewText,
            cancellationToken);
        var normalizedChapters = NormalizeChapters(book.Title, book.PageCount, aiChapters.Count > 0 ? aiChapters : detectedChapters);
        var createdAt = DateTime.UtcNow;
        var chapters = normalizedChapters
            .Select((chapter, index) => new StudyChapter
            {
                Id = Guid.NewGuid().ToString("N"),
                BookId = book.Id,
                Title = chapter.Title,
                StartPage = chapter.StartPage,
                EndPage = chapter.EndPage,
                SortOrder = index,
                CreatedAt = createdAt
            })
            .ToList();

        foreach (var chapter in chapters)
        {
            await _studyRepository.InsertChapterAsync(chapter, cancellationToken);
        }

        return chapters;
    }

    private async Task<IReadOnlyList<DetectedChapterInput>> DetectChaptersWithAiAsync(
        string bookTitle,
        int pageCount,
        IReadOnlyList<DetectedChapterInput> detectedChapters,
        string? tableOfContentsText,
        string? previewText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tableOfContentsText) &&
            string.IsNullOrWhiteSpace(previewText) &&
            detectedChapters.Count == 0)
        {
            return [];
        }

        await _modelProvider.EnsureInitializedAsync(cancellationToken);
        var status = _modelProvider.GetStatus();
        if (!string.Equals(status.Status, "Ready", StringComparison.OrdinalIgnoreCase))
            return [];

        var prompt = BuildPrompt(bookTitle, pageCount, detectedChapters, tableOfContentsText, previewText);
        var rawResponse = new StringBuilder();

        try
        {
            await using var executor = await _modelProvider.CreateExecutorAsync(cancellationToken);
            await foreach (var token in executor.InferAsync(prompt, BuildInferenceOptions(), cancellationToken))
            {
                rawResponse.Append(token);
            }

            return ParseChapterResponse(rawResponse.ToString(), pageCount);
        }
        catch (ValidationException)
        {
            return [];
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }

    private async Task EnsureAutoPlanAsync(
        StudyBook book,
        IReadOnlyList<StudyChapter> chapters,
        CancellationToken cancellationToken)
    {
        if (chapters.Count == 0)
            return;

        var plans = await _studyRepository.GetAllPlansAsync(cancellationToken);
        var targetPlan = plans
            .Where(plan => string.Equals(plan.BookId, book.Id, StringComparison.Ordinal))
            .Where(plan => !string.Equals(plan.Status, StudyPlanStatus.Completed, StringComparison.Ordinal))
            .OrderBy(plan => GetPlanPriority(plan.Status))
            .ThenByDescending(plan => plan.UpdatedAt)
            .FirstOrDefault();

        if (targetPlan is null)
        {
            var occupiedDates = await GetOccupiedDatesAsync(plans, null, cancellationToken);
            var scheduledDates = GetAvailableDates(DateTime.UtcNow.Date, chapters.Count, skipWeekends: true, occupiedDates);
            var now = DateTime.UtcNow;
            targetPlan = new StudyPlan
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = $"{book.Title} study plan",
                BookId = book.Id,
                StartDate = scheduledDates[0],
                EndDate = scheduledDates[^1],
                Status = StudyPlanStatus.Active,
                SkipWeekends = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _studyRepository.InsertPlanAsync(targetPlan, cancellationToken);
            await _studyRepository.InsertPlanItemsAsync(
                CreatePlanItems(targetPlan.Id, chapters, scheduledDates, sortOrderOffset: 0),
                cancellationToken);
            return;
        }

        var existingItems = await _studyRepository.GetPlanItemsByPlanIdAsync(targetPlan.Id, cancellationToken);
        var existingChapterIds = existingItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ChapterId))
            .Select(item => item.ChapterId!)
            .ToHashSet(StringComparer.Ordinal);

        var missingChapters = chapters
            .Where(chapter => !existingChapterIds.Contains(chapter.Id))
            .OrderBy(chapter => chapter.SortOrder)
            .ThenBy(chapter => chapter.StartPage)
            .ToList();

        if (missingChapters.Count == 0)
        {
            if (!string.Equals(targetPlan.Status, StudyPlanStatus.Active, StringComparison.Ordinal) &&
                existingItems.Count > 0)
            {
                await _studyRepository.UpdatePlanAsync(new StudyPlan
                {
                    Id = targetPlan.Id,
                    Title = targetPlan.Title,
                    BookId = targetPlan.BookId,
                    StartDate = targetPlan.StartDate,
                    EndDate = targetPlan.EndDate,
                    Status = StudyPlanStatus.Active,
                    SkipWeekends = targetPlan.SkipWeekends,
                    CreatedAt = targetPlan.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return;
        }

        var existingLatestDate = existingItems.Count == 0
            ? targetPlan.StartDate.Date
            : existingItems.Max(item => item.ScheduledDate.Date).AddDays(1);
        var scheduleStart = MaxDate(DateTime.UtcNow.Date, targetPlan.StartDate.Date, existingLatestDate);
        var reservedDates = await GetOccupiedDatesAsync(plans, targetPlan.Id, cancellationToken);
        var newDates = GetAvailableDates(scheduleStart, missingChapters.Count, targetPlan.SkipWeekends, reservedDates);

        await _studyRepository.InsertPlanItemsAsync(
            CreatePlanItems(targetPlan.Id, missingChapters, newDates, existingItems.Count),
            cancellationToken);

        var updatedEndDate = MaxDate(targetPlan.EndDate.Date, newDates[^1]);
        if (updatedEndDate != targetPlan.EndDate.Date ||
            !string.Equals(targetPlan.Status, StudyPlanStatus.Active, StringComparison.Ordinal))
        {
            await _studyRepository.UpdatePlanAsync(new StudyPlan
            {
                Id = targetPlan.Id,
                Title = targetPlan.Title,
                BookId = targetPlan.BookId,
                StartDate = targetPlan.StartDate,
                EndDate = updatedEndDate,
                Status = StudyPlanStatus.Active,
                SkipWeekends = targetPlan.SkipWeekends,
                CreatedAt = targetPlan.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    private async Task<HashSet<DateTime>> GetOccupiedDatesAsync(
        IReadOnlyList<StudyPlan> plans,
        string? excludedPlanId,
        CancellationToken cancellationToken)
    {
        var occupiedDates = new HashSet<DateTime>();
        var today = DateTime.UtcNow.Date;

        foreach (var plan in plans)
        {
            if (string.Equals(plan.Id, excludedPlanId, StringComparison.Ordinal) ||
                string.Equals(plan.Status, StudyPlanStatus.Completed, StringComparison.Ordinal))
            {
                continue;
            }

            var items = await _studyRepository.GetPlanItemsByPlanIdAsync(plan.Id, cancellationToken);
            foreach (var item in items)
            {
                if (item.ScheduledDate.Date < today)
                    continue;

                if (item.Status is PlanItemStatus.Done or PlanItemStatus.Skipped)
                    continue;

                occupiedDates.Add(item.ScheduledDate.Date);
            }
        }

        return occupiedDates;
    }

    private static IReadOnlyList<StudyPlanItem> CreatePlanItems(
        string planId,
        IReadOnlyList<StudyChapter> chapters,
        IReadOnlyList<DateTime> scheduledDates,
        int sortOrderOffset)
        => chapters
            .Select((chapter, index) => new StudyPlanItem
            {
                Id = Guid.NewGuid().ToString("N"),
                PlanId = planId,
                ChapterId = chapter.Id,
                Title = chapter.Title,
                ScheduledDate = scheduledDates[index],
                Status = PlanItemStatus.Pending,
                SortOrder = sortOrderOffset + index,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

    private static List<DateTime> GetAvailableDates(
        DateTime startDate,
        int requiredCount,
        bool skipWeekends,
        IReadOnlySet<DateTime> occupiedDates)
    {
        var dates = new List<DateTime>(requiredCount);
        for (var day = startDate.Date; dates.Count < requiredCount; day = day.AddDays(1))
        {
            if (skipWeekends && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            if (occupiedDates.Contains(day))
                continue;

            dates.Add(day);
        }

        return dates;
    }

    private static IReadOnlyList<(string Title, int StartPage, int EndPage)> NormalizeChapters(
        string bookTitle,
        int pageCount,
        IReadOnlyList<DetectedChapterInput> chapters)
    {
        var groupedByPage = chapters
            .Select(chapter => new
            {
                Title = chapter.Title.Trim(),
                StartPage = Math.Clamp(chapter.StartPage, 1, pageCount),
                EndPage = Math.Clamp(chapter.EndPage, 1, pageCount)
            })
            .Where(chapter => !string.IsNullOrWhiteSpace(chapter.Title))
            .OrderBy(chapter => chapter.StartPage)
            .ThenBy(chapter => chapter.Title, StringComparer.OrdinalIgnoreCase)
            .GroupBy(chapter => chapter.StartPage)
            .Select(group => group
                .OrderByDescending(chapter => GetChapterPriority(chapter.Title))
                .ThenByDescending(chapter => chapter.Title.Length)
                .ThenBy(chapter => chapter.Title, StringComparer.OrdinalIgnoreCase)
                .First())
            .ToList();

        if (groupedByPage.Count == 0)
        {
            return
            [
                ($"Complete {bookTitle}", 1, pageCount)
            ];
        }

        var normalized = groupedByPage;
        var specificStudyUnitCount = normalized.Count(chapter => GetChapterPriority(chapter.Title) >= 60);
        if (specificStudyUnitCount >= 3)
        {
            var filtered = normalized
                .Where(chapter => !IsSkippableMatter(chapter.Title))
                .ToList();

            if (filtered.Count > 0)
                normalized = filtered;
        }

        if (normalized.Count(chapter => GetChapterPriority(chapter.Title) >= 80) >= 3)
        {
            var withoutContainers = normalized
                .Where(chapter => !Regex.IsMatch(chapter.Title, @"^\s*(part|section|book|volume)\b", RegexOptions.IgnoreCase))
                .ToList();

            if (withoutContainers.Count > 0)
                normalized = withoutContainers;
        }

        var results = new List<(string Title, int StartPage, int EndPage)>();
        for (var index = 0; index < normalized.Count; index++)
        {
            var current = normalized[index];
            var nextStartPage = index + 1 < normalized.Count
                ? normalized[index + 1].StartPage
                : pageCount + 1;
            var endPage = Math.Clamp(
                Math.Min(Math.Max(current.EndPage, current.StartPage), nextStartPage - 1),
                current.StartPage,
                pageCount);

            results.Add((current.Title, current.StartPage, endPage));
        }

        return results;
    }

    private static int GetPlanPriority(string status)
        => status switch
        {
            StudyPlanStatus.Active => 0,
            StudyPlanStatus.Draft => 1,
            StudyPlanStatus.Paused => 2,
            _ => 3
        };

    private static DateTime MaxDate(params DateTime[] values)
        => values.Max(value => value.Date);

    private static AssistantPromptModel BuildPrompt(
        string bookTitle,
        int pageCount,
        IReadOnlyList<DetectedChapterInput> detectedChapters,
        string? tableOfContentsText,
        string? previewText)
    {
        var candidateText = detectedChapters.Count == 0
            ? "None"
            : string.Join(
                Environment.NewLine,
                detectedChapters.Select(chapter => $"- {chapter.Title} ({chapter.StartPage}-{chapter.EndPage})"));

        return new AssistantPromptModel(
            "You extract the real study units from noisy PDF metadata. Return ONLY valid JSON: an array of objects with \"title\" and \"startPage\". Do not include markdown or commentary.",
            $"""
            Detect the study units for this PDF book.

            Book title: {bookTitle}
            Total pages: {pageCount}

            Heuristic chapter candidates:
            {TrimForPrompt(candidateText, 4000)}

            Table of contents text:
            {TrimForPrompt(tableOfContentsText, 7000)}

            Front matter / early pages text:
            {TrimForPrompt(previewText, 5000)}

            Requirements:
            - Return a JSON array only.
            - Each object must have title, startPage.
            - Pages must be integers between 1 and {pageCount}.
            - Return the real units a learner should study in order.
            - Book title and table of contents can be in English, Arabic, or another language. Maintain the original language of the titles exactly.
            - Prefer leaf reading units over containers.
            - If both a container title and a more specific child title exist for the same region, keep the more specific child title.
            - Prefer chapter, lesson, module, unit, appendix, lecture, case, or similarly sized study units (including equivalents like فصل, باب, درس).
            - If the book does not use the word "chapter", infer the equivalent study units from the table of contents.
            - Do not return title page, copyright, dedication, contents, table of contents, foreword, preface, acknowledgments, about the author, glossary, bibliography, references, or index unless the source clearly treats them as study units.
            - Do not return tiny subsections like Conclusion, Summary, Tests, or Case Study when they are clearly nested inside a larger chapter-level unit.
            - Use actual titles from the source. Do not invent filler labels like "Introduction" unless that title is explicitly present.
            - If the content only exposes larger containers at first, use the table of contents text to recover the more specific study units underneath them whenever possible.
            """");
    }

    private static AssistantInferenceOptionsModel BuildInferenceOptions()
        => new(
            1200,
            0.15f,
            ["<|im_end|>", "<|endoftext|>", "<|im_start|>user", "<|im_start|>system"]);

    private static List<DetectedChapterInput> ParseChapterResponse(string response, int pageCount)
    {
        var trimmed = response.Trim();
        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            trimmed = trimmed[arrayStart..(arrayEnd + 1)];
        }

        try
        {
            var chapters = JsonSerializer.Deserialize<List<DetectedChapterResponse>>(trimmed, JsonOptions) ?? [];
            return chapters
                .Where(chapter => !string.IsNullOrWhiteSpace(chapter.Title))
                .Select(chapter => new DetectedChapterInput(
                    chapter.Title.Trim(),
                    Math.Clamp(chapter.StartPage, 1, pageCount),
                    Math.Clamp(Math.Max(chapter.EndPage, chapter.StartPage), 1, pageCount)))
                .Where(chapter => chapter.EndPage >= chapter.StartPage)
                .Take(64)
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new ValidationException([new ValidationFailure("response", $"The local assistant returned invalid chapter JSON. {ex.Message}")]);
        }
    }

    private static string TrimForPrompt(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "None";

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : $"{trimmed[..maxLength]}{Environment.NewLine}[truncated]";
    }

    private static bool IsSkippableMatter(string title)
        => Regex.IsMatch(
            title,
            @"^\s*(title page|half title|cover|copyright|dedication|contents?|table of contents|foreword|preface|acknowledg(e)?ments?|about the author|about the authors|introduction to the .* edition|index|glossary|bibliography|references?|المقدمة|مقدمة|تمهيد|الفهرس|فهرس|محتويات|إهداء|المراجع|الفهارس)\b",
            RegexOptions.IgnoreCase);

    private static int GetChapterPriority(string title)
    {
        var normalized = title.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return 0;

        if (IsSkippableMatter(normalized))
            return 0;

        if (Regex.IsMatch(normalized, @"^\s*(chapter|appendix|lesson|module|unit|lecture|session|topic|act|فصل|الفصل|درس|الدرس|باب|الباب|موضوع|الموضوع)\b", RegexOptions.IgnoreCase))
            return 100;

        if (Regex.IsMatch(normalized, @"^\s*\d+\s*[:.)-]?\s+\S", RegexOptions.IgnoreCase))
            return 90;

        if (Regex.IsMatch(normalized, @"^\s*(part|section|book|volume|مجلد|كتاب|جزء|قسم)\b", RegexOptions.IgnoreCase))
            return 35;

        if (Regex.IsMatch(normalized, @"^\s*(conclusion|summary|tests?|exercises?|solutions?|case study|epilogue|afterword|ملخص|خلاصة|خاتمة|تمرين|تمارين|أسئلة)\b", RegexOptions.IgnoreCase))
            return 10;

        var wordCount = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length;

        return wordCount switch
        {
            >= 5 => 75,
            >= 3 => 60,
            2 => 25,
            _ => 15
        };
    }

    private sealed record DetectedChapterResponse(string Title, int StartPage, int EndPage);
}
