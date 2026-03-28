using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UploadChapterAudioCommand(string BookId, string ChapterId, IFormFile AudioFile) : IRequest;

public sealed class UploadChapterAudioCommandHandler : IRequestHandler<UploadChapterAudioCommand>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IWebHostEnvironment _environment;

    public UploadChapterAudioCommandHandler(IStudyRepository studyRepository, IWebHostEnvironment environment)
    {
        _studyRepository = studyRepository;
        _environment = environment;
    }

    public async Task Handle(UploadChapterAudioCommand request, CancellationToken cancellationToken)
    {
        _ = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
        var chapter = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");

        var extension = Path.GetExtension(request.AudioFile.FileName);
        var relativeDirectory = Path.Combine("data", "study", "audio", request.BookId);
        var absoluteDirectory = Path.Combine(_environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        if (!string.IsNullOrWhiteSpace(chapter.AudioFilePath))
        {
            var existingPath = Path.Combine(_environment.ContentRootPath, chapter.AudioFilePath);
            if (File.Exists(existingPath))
                File.Delete(existingPath);
        }

        var fileName = $"{request.ChapterId}{extension}";
        var relativePath = Path.Combine(relativeDirectory, fileName);
        var absolutePath = Path.Combine(_environment.ContentRootPath, relativePath);

        await using (var stream = File.Create(absolutePath))
        {
            await request.AudioFile.CopyToAsync(stream, cancellationToken);
        }

        var updatedChapter = new StudyChapter
        {
            Id = chapter.Id,
            BookId = chapter.BookId,
            Title = chapter.Title,
            StartPage = chapter.StartPage,
            EndPage = chapter.EndPage,
            SortOrder = chapter.SortOrder,
            AudioFileName = request.AudioFile.FileName,
            AudioFilePath = relativePath,
            Notes = chapter.Notes,
            CreatedAt = chapter.CreatedAt
        };

        await _studyRepository.UpdateChapterAsync(updatedChapter, cancellationToken);
    }
}