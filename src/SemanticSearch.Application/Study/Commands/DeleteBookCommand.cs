using MediatR;
using Microsoft.AspNetCore.Hosting;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record DeleteBookCommand(string BookId) : IRequest;

public sealed class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IWebHostEnvironment _environment;

    public DeleteBookCommandHandler(IStudyRepository studyRepository, IWebHostEnvironment environment)
    {
        _studyRepository = studyRepository;
        _environment = environment;
    }

    public async Task Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var existingBook = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");

        await _studyRepository.DeleteBookAsync(request.BookId, cancellationToken);

        var absolutePdfPath = Path.Combine(_environment.ContentRootPath, existingBook.FilePath);
        var bookDirectory = Path.GetDirectoryName(absolutePdfPath);
        if (!string.IsNullOrWhiteSpace(bookDirectory) && Directory.Exists(bookDirectory))
            Directory.Delete(bookDirectory, true);

        var audioDirectory = Path.Combine(_environment.ContentRootPath, "data", "study", "audio", request.BookId);
        if (Directory.Exists(audioDirectory))
            Directory.Delete(audioDirectory, true);
    }
}
