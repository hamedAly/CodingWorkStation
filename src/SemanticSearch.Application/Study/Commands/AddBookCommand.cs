using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record AddBookCommand(
    string Title,
    string? Author,
    string? Description,
    IFormFile PdfFile) : IRequest<BookDetailModel>;

public sealed class AddBookCommandHandler : IRequestHandler<AddBookCommand, BookDetailModel>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IWebHostEnvironment _environment;

    public AddBookCommandHandler(IStudyRepository studyRepository, IWebHostEnvironment environment)
    {
        _studyRepository = studyRepository;
        _environment = environment;
    }

    public async Task<BookDetailModel> Handle(AddBookCommand request, CancellationToken cancellationToken)
    {
        const long maxPdfBytes = 209_715_200;
        if (request.PdfFile.Length > maxPdfBytes)
            throw new PayloadTooLargeException("PDF uploads must be 200 MB or smaller.");

        var id = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(request.PdfFile.FileName);
        var safeFileName = string.IsNullOrWhiteSpace(extension) ? "book.pdf" : $"book{extension}";
        var relativePath = Path.Combine("data", "study", "books", id, safeFileName);
        var absolutePath = Path.Combine(_environment.ContentRootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (var fileStream = File.Create(absolutePath))
        {
            await request.PdfFile.CopyToAsync(fileStream, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var book = new StudyBook
        {
            Id = id,
            Title = request.Title.Trim(),
            Author = request.Author?.Trim(),
            Description = request.Description?.Trim(),
            FileName = request.PdfFile.FileName,
            FilePath = relativePath,
            PageCount = 1,
            LastReadPage = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _studyRepository.InsertBookAsync(book, cancellationToken);
        return book.ToModel([]);
    }
}
