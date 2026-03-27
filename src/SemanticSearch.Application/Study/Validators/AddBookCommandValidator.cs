using FluentValidation;
using SemanticSearch.Application.Study.Commands;

namespace SemanticSearch.Application.Study.Validators;

public sealed class AddBookCommandValidator : AbstractValidator<AddBookCommand>
{
    public AddBookCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Author)
            .MaximumLength(300).WithMessage("Author must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.PdfFile)
            .NotNull().WithMessage("A PDF file is required.")
            .Must(file => file.Length > 0).WithMessage("The uploaded PDF is empty.")
            .Must(file => file.Length <= 209_715_200).WithMessage("PDF uploads must be 200 MB or smaller.")
            .Must(file => string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF files are supported.")
            .Must(HasPdfHeader).WithMessage("The uploaded file does not appear to be a valid PDF.");
    }

    private static bool HasPdfHeader(Microsoft.AspNetCore.Http.IFormFile file)
    {
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[5];
        if (stream.Read(header) < 5)
            return false;

        return header.SequenceEqual("%PDF-"u8);
    }
}

public sealed class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");
        RuleFor(x => x.Author)
            .MaximumLength(300).WithMessage("Author must not exceed 300 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
    }
}

public sealed class UpdateLastReadPageCommandValidator : AbstractValidator<UpdateLastReadPageCommand>
{
    public UpdateLastReadPageCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}
