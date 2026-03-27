using FluentValidation;
using SemanticSearch.Application.Study.Commands;

namespace SemanticSearch.Application.Study.Validators;

public sealed class UploadChapterAudioCommandValidator : AbstractValidator<UploadChapterAudioCommand>
{
    private static readonly string[] AllowedExtensions = [".mp3", ".wav", ".m4a"];

    public UploadChapterAudioCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
        RuleFor(x => x.AudioFile)
            .NotNull().WithMessage("An audio file is required.")
            .Must(file => file.Length > 0).WithMessage("The uploaded audio file is empty.")
            .Must(file => file.Length <= 104_857_600).WithMessage("Audio uploads must be 100 MB or smaller.")
            .Must(file => AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)
                || file.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only MP3, WAV, and M4A audio files are supported.");
    }
}