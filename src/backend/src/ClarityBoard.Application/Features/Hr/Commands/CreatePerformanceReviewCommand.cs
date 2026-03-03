using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.view")]
public record CreatePerformanceReviewCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required Guid ReviewerId { get; init; }
    public required DateOnly ReviewPeriodStart { get; init; }
    public required DateOnly ReviewPeriodEnd { get; init; }
    public required string ReviewType { get; init; }
}

public class CreatePerformanceReviewCommandValidator : AbstractValidator<CreatePerformanceReviewCommand>
{
    public CreatePerformanceReviewCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.ReviewPeriodEnd).GreaterThanOrEqualTo(x => x.ReviewPeriodStart)
            .WithMessage("ReviewPeriodEnd must be >= ReviewPeriodStart.");
        RuleFor(x => x.ReviewType)
            .NotEmpty()
            .Must(t => Enum.TryParse<ReviewType>(t, ignoreCase: true, out _))
            .WithMessage("ReviewType must be one of: Annual, Probation, Quarterly, ThreeSixty.");
    }
}

public class CreatePerformanceReviewCommandHandler : IRequestHandler<CreatePerformanceReviewCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreatePerformanceReviewCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreatePerformanceReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewType = Enum.Parse<ReviewType>(request.ReviewType, ignoreCase: true);

        var review = PerformanceReview.Create(
            employeeId:  request.EmployeeId,
            reviewerId:  request.ReviewerId,
            periodStart: request.ReviewPeriodStart,
            periodEnd:   request.ReviewPeriodEnd,
            type:        reviewType);

        _db.PerformanceReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
