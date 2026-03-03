using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.view")]
public record CompletePerformanceReviewCommand : IRequest<Unit>
{
    public required Guid ReviewId { get; init; }
    public required int OverallRating { get; init; }
    public required string StrengthsNotes { get; init; }
    public required string ImprovementNotes { get; init; }
    public required string GoalsNotes { get; init; }
}

public class CompletePerformanceReviewCommandValidator : AbstractValidator<CompletePerformanceReviewCommand>
{
    public CompletePerformanceReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.OverallRating).InclusiveBetween(1, 5);
        RuleFor(x => x.StrengthsNotes).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ImprovementNotes).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.GoalsNotes).NotEmpty().MaximumLength(2000);
    }
}

public class CompletePerformanceReviewCommandHandler : IRequestHandler<CompletePerformanceReviewCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CompletePerformanceReviewCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CompletePerformanceReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _db.PerformanceReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken)
            ?? throw new InvalidOperationException($"Performance review '{request.ReviewId}' not found.");

        var employee = await _db.Employees.FindAsync([review.EmployeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", review.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new InvalidOperationException("Access denied to this review.");

        if (review.Status == ReviewStatus.Completed)
            throw new InvalidOperationException("Review is already completed.");

        review.Complete(
            rating:      request.OverallRating,
            strengths:   request.StrengthsNotes,
            improvement: request.ImprovementNotes,
            goals:       request.GoalsNotes);

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
