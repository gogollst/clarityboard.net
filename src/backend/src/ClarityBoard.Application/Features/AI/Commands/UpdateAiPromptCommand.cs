using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Commands;

public record UpdateAiPromptCommand : IRequest<Unit>
{
    public required string PromptKey { get; init; }
    public required string SystemPrompt { get; init; }
    public string? UserPromptTemplate { get; init; }
    public AiProvider PrimaryProvider { get; init; }
    public required string PrimaryModel { get; init; }
    public AiProvider FallbackProvider { get; init; }
    public required string FallbackModel { get; init; }
    public decimal Temperature { get; init; }
    public int MaxTokens { get; init; }
    public required string ChangeSummary { get; init; }
}

public class UpdateAiPromptCommandValidator : AbstractValidator<UpdateAiPromptCommand>
{
    public UpdateAiPromptCommandValidator()
    {
        RuleFor(x => x.PromptKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SystemPrompt).NotEmpty();
        RuleFor(x => x.PrimaryModel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FallbackModel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Temperature).InclusiveBetween(0m, 1m);
        RuleFor(x => x.MaxTokens).InclusiveBetween(1, 100_000);
        RuleFor(x => x.ChangeSummary).NotEmpty().MaximumLength(500)
            .WithMessage("ChangeSummary is required and must not exceed 500 characters.");
    }
}

public class UpdateAiPromptCommandHandler : IRequestHandler<UpdateAiPromptCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;

    public UpdateAiPromptCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, ICacheService cache)
    {
        _db          = db;
        _currentUser = currentUser;
        _cache       = cache;
    }

    public async Task<Unit> Handle(
        UpdateAiPromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = await _db.AiPrompts
            .FirstOrDefaultAsync(p => p.PromptKey == request.PromptKey, cancellationToken)
            ?? throw new KeyNotFoundException($"Prompt '{request.PromptKey}' not found.");

        // Snapshot current version before overwriting
        var version = AiPromptVersion.Create(
            prompt.Id,
            prompt.Version,
            prompt.SystemPrompt,
            prompt.UserPromptTemplate,
            prompt.PrimaryProvider,
            prompt.FallbackProvider,
            request.ChangeSummary,
            _currentUser.UserId);

        _db.AiPromptVersions.Add(version);

        prompt.Update(
            request.SystemPrompt,
            request.UserPromptTemplate,
            request.PrimaryProvider,
            request.PrimaryModel,
            request.FallbackProvider,
            request.FallbackModel,
            request.Temperature,
            request.MaxTokens,
            _currentUser.UserId);

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate cache so the next AI call picks up the new prompt
        await _cache.RemoveAsync($"ai:prompt:{request.PromptKey}");

        return Unit.Value;
    }
}

