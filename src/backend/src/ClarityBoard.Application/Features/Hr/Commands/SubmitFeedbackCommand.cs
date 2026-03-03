using System.Text.Json;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record SubmitFeedbackCommand : IRequest<Guid>
{
    public required Guid ReviewId { get; init; }
    public required string RespondentType { get; init; }
    public required bool IsAnonymous { get; init; }
    public required int Rating { get; init; }
    public string? Comments { get; init; }
    public Dictionary<string, int>? CompetencyScores { get; init; }
}

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.RespondentType)
            .NotEmpty()
            .Must(t => Enum.TryParse<RespondentType>(t, ignoreCase: true, out _))
            .WithMessage("RespondentType must be one of: Self, Peer, Manager, DirectReport.");
        RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
    }
}

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SubmitFeedbackCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var review = await _db.PerformanceReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken)
            ?? throw new InvalidOperationException($"Performance review '{request.ReviewId}' not found.");

        var employee = await _db.Employees.FindAsync([review.EmployeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", review.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new InvalidOperationException("Access denied to this review.");

        if (review.Status == ReviewStatus.Completed)
            throw new InvalidOperationException("Cannot submit feedback on a completed review.");

        var respondentType = Enum.Parse<RespondentType>(request.RespondentType, ignoreCase: true);

        var entry = FeedbackEntry.Create(
            reviewId:    request.ReviewId,
            respondentId: _currentUser.UserId,
            type:         respondentType,
            isAnonymous:  request.IsAnonymous);

        var competencyScoresJson = request.CompetencyScores != null
            ? JsonSerializer.Serialize(request.CompetencyScores)
            : null;

        entry.Submit(request.Rating, request.Comments, competencyScoresJson);

        if (review.Status == ReviewStatus.Draft)
            review.StartProgress();

        _db.FeedbackEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
