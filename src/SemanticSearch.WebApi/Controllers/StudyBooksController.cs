using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Application.Study.Queries;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.WebApi.Contracts.Study;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/study/books")]
public sealed class StudyBooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStudyRepository _studyRepository;
    private readonly IWebHostEnvironment _environment;

    public StudyBooksController(IMediator mediator, IStudyRepository studyRepository, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _studyRepository = studyRepository;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok((await _mediator.Send(new ListBooksQuery(), cancellationToken)).Select(MapBookSummary).ToList());

    [HttpPost]
    [ProducesResponseType(typeof(BookDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(209_715_200)]
    public async Task<IActionResult> Add([FromForm] AddBookForm form, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddBookCommand(form.Title, form.Author, form.Description, form.PdfFile), cancellationToken);
        return CreatedAtAction(nameof(Get), new { bookId = result.Id }, MapBookDetail(result));
    }

    [HttpGet("{bookId}")]
    [ProducesResponseType(typeof(BookDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string bookId, CancellationToken cancellationToken)
        => Ok(MapBookDetail(await _mediator.Send(new GetBookQuery(bookId), cancellationToken)));

    [HttpPut("{bookId}")]
    [ProducesResponseType(typeof(BookDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] string bookId, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
        => Ok(MapBookDetail(await _mediator.Send(new UpdateBookCommand(bookId, request.Title, request.Author, request.Description), cancellationToken)));

    [HttpDelete("{bookId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] string bookId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBookCommand(bookId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{bookId}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pdf([FromRoute] string bookId, CancellationToken cancellationToken)
    {
        var book = await _studyRepository.GetBookByIdAsync(bookId, cancellationToken);
        if (book is null)
            return NotFound();

        var absolutePath = Path.Combine(_environment.ContentRootPath, book.FilePath);
        if (!System.IO.File.Exists(absolutePath))
            return NotFound();

        return PhysicalFile(absolutePath, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPatch("{bookId}/last-read-page")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLastReadPage([FromRoute] string bookId, [FromBody] UpdateLastReadPageRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateLastReadPageCommand(bookId, request.Page), cancellationToken);
        return NoContent();
    }

    [HttpPost("{bookId}/auto-setup")]
    [ProducesResponseType(typeof(BookDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> FinalizeImport([FromRoute] string bookId, [FromBody] FinalizeBookImportRequest request, CancellationToken cancellationToken)
        => Ok(MapBookDetail(await _mediator.Send(
            new FinalizeBookImportCommand(
                bookId,
                request.PageCount,
                request.Chapters.Select(chapter => new DetectedChapterInput(chapter.Title, chapter.StartPage, chapter.EndPage)).ToList(),
                request.TableOfContentsText,
                request.PreviewText),
            cancellationToken)));

    [HttpPost("{bookId}/chapters")]
    [ProducesResponseType(typeof(ChapterResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddChapter([FromRoute] string bookId, [FromBody] AddChapterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddChapterCommand(bookId, request.Title, request.StartPage, request.EndPage), cancellationToken);
        return CreatedAtAction(nameof(Get), new { bookId }, MapChapter(result));
    }

    [HttpPut("{bookId}/chapters/{chapterId}")]
    [ProducesResponseType(typeof(ChapterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateChapter([FromRoute] string bookId, [FromRoute] string chapterId, [FromBody] UpdateChapterRequest request, CancellationToken cancellationToken)
        => Ok(MapChapter(await _mediator.Send(new UpdateChapterCommand(bookId, chapterId, request.Title, request.StartPage, request.EndPage), cancellationToken)));

    [HttpDelete("{bookId}/chapters/{chapterId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteChapter([FromRoute] string bookId, [FromRoute] string chapterId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteChapterCommand(bookId, chapterId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{bookId}/chapters/{chapterId}/notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateChapterNotes([FromRoute] string bookId, [FromRoute] string chapterId, [FromBody] UpdateChapterNotesRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateChapterNotesCommand(bookId, chapterId, request.Notes), cancellationToken);
        return NoContent();
    }

    [HttpPost("{bookId}/chapters/{chapterId}/audio")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UploadAudio([FromRoute] string bookId, [FromRoute] string chapterId, [FromForm] UploadChapterAudioForm form, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UploadChapterAudioCommand(bookId, chapterId, form.AudioFile), cancellationToken);
        return NoContent();
    }

    [HttpGet("{bookId}/chapters/{chapterId}/audio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Audio([FromRoute] string bookId, [FromRoute] string chapterId, CancellationToken cancellationToken)
    {
        var chapter = await _studyRepository.GetChapterByIdAsync(chapterId, cancellationToken);
        if (chapter is null || !string.Equals(chapter.BookId, bookId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(chapter.AudioFilePath))
            return NotFound();

        var absolutePath = Path.Combine(_environment.ContentRootPath, chapter.AudioFilePath);
        if (!System.IO.File.Exists(absolutePath))
            return NotFound();

        return PhysicalFile(absolutePath, ResolveAudioContentType(chapter.AudioFilePath), enableRangeProcessing: true);
    }

    private static BookSummaryResponse MapBookSummary(BookSummaryModel model)
        => new(model.Id, model.Title, model.Author, model.PageCount, model.LastReadPage, model.ChapterCount, model.CreatedAt);

    private static BookDetailResponse MapBookDetail(BookDetailModel model)
        => new(model.Id, model.Title, model.Author, model.Description, model.FileName, model.PageCount, model.LastReadPage, model.Chapters.Select(MapChapter).ToList(), model.CreatedAt, model.UpdatedAt);

    private static ChapterResponse MapChapter(ChapterModel model)
        => new(model.Id, model.Title, model.StartPage, model.EndPage, model.SortOrder, model.HasAudio, model.HasNotes);

    private static string ResolveAudioContentType(string audioPath)
        => Path.GetExtension(audioPath).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream"
        };

    public sealed class AddBookForm
    {
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Description { get; set; }
        public IFormFile PdfFile { get; set; } = default!;
    }

    public sealed class UploadChapterAudioForm
    {
        public IFormFile AudioFile { get; set; } = default!;
    }
}
