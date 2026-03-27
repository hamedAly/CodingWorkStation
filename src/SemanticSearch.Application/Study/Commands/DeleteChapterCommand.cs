using MediatR;
using Microsoft.AspNetCore.Hosting;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record DeleteChapterCommand(string BookId, string ChapterId) : IRequest;

public sealed class DeleteChapterCommandHandler : IRequestHandler<DeleteChapterCommand>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IWebHostEnvironment _environment;

    public DeleteChapterCommandHandler(IStudyRepository studyRepository, IWebHostEnvironment environment)
    {
        _studyRepository = studyRepository;
        _environment = environment;
    }

    public async Task Handle(DeleteChapterCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _studyRepository.GetChapterByIdAsync(request.ChapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{request.ChapterId}' was not found.");

        await _studyRepository.DeleteChapterAsync(request.ChapterId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(chapter.AudioFilePath))
        {
            var audioPath = Path.Combine(_environment.ContentRootPath, chapter.AudioFilePath);
            if (File.Exists(audioPath))
                File.Delete(audioPath);
        }
    }
}
