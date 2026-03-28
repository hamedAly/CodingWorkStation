using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdatePlanItemStatusCommand(string PlanId, string ItemId, string Status) : IRequest<PlanItemModel>;

public sealed class UpdatePlanItemStatusCommandHandler : IRequestHandler<UpdatePlanItemStatusCommand, PlanItemModel>
{
    private readonly IStudyRepository _studyRepository;

    public UpdatePlanItemStatusCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<PlanItemModel> Handle(UpdatePlanItemStatusCommand request, CancellationToken cancellationToken)
    {
        _ = await _studyRepository.GetPlanByIdAsync(request.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Study plan '{request.PlanId}' was not found.");

        if (!PlanItemStatus.All.Contains(request.Status))
            throw new InvalidOperationException("Invalid plan item status.");

        var items = await _studyRepository.GetPlanItemsByPlanIdAsync(request.PlanId, cancellationToken);
        var item = items.FirstOrDefault(candidate => candidate.Id == request.ItemId)
            ?? throw new NotFoundException($"Study plan item '{request.ItemId}' was not found.");

        DateTime? completedDate = request.Status == PlanItemStatus.Done ? DateTime.UtcNow : null;
        await _studyRepository.UpdatePlanItemStatusAsync(request.ItemId, request.Status, completedDate, cancellationToken);
        return new PlanItemModel(
            item.Id,
            item.ChapterId,
            item.Title,
            item.ScheduledDate,
            request.Status,
            completedDate,
            item.SortOrder);
    }
}
