using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Commands;

public sealed record CreateStudyPlanCommand(string Title, string? BookId, DateTime StartDate, DateTime EndDate, bool SkipWeekends) : IRequest<StudyPlanDetailModel>;

public sealed class CreateStudyPlanCommandHandler : IRequestHandler<CreateStudyPlanCommand, StudyPlanDetailModel>
{
    private readonly IStudyRepository _studyRepository;

    public CreateStudyPlanCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<StudyPlanDetailModel> Handle(CreateStudyPlanCommand request, CancellationToken cancellationToken)
    {
        string? bookTitle = null;
        if (!string.IsNullOrWhiteSpace(request.BookId))
        {
            var book = await _studyRepository.GetBookByIdAsync(request.BookId, cancellationToken)
                ?? throw new NotFoundException($"Study book '{request.BookId}' was not found.");
            bookTitle = book.Title;
        }

        var now = DateTime.UtcNow;
        var plan = new StudyPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            BookId = request.BookId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = StudyPlanStatus.Draft,
            SkipWeekends = request.SkipWeekends,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _studyRepository.InsertPlanAsync(plan, cancellationToken);
        return new StudyPlanDetailModel(plan.Id, plan.Title, plan.BookId, bookTitle, plan.StartDate, plan.EndDate, plan.Status, plan.SkipWeekends, [], 0d);
    }
}
