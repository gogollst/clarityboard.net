using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Commands;

public record RestorePromptVersionCommand : IRequest<Unit>
{
    public required string PromptKey { get; init; }
    public int Version { get; init; }
}

public class RestorePromptVersionCommandHandler : IRequestHandler<RestorePromptVersionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;

    public RestorePromptVersionCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, ICacheService cache)
    {
        _db          = db;
        _currentUser = currentUser;
        _cache       = cache;
    }

    public async Task<Unit> Handle(
        RestorePromptVersionCommand request, CancellationToken cancellationToken)
    {
        var prompt = await _db.AiPrompts
            .FirstOrDefaultAsync(p => p.PromptKey == request.PromptKey, cancellationToken)
            ?? throw new KeyNotFoundException($"Prompt '{request.PromptKey}' not found.");

        var oldVersion = await _db.AiPromptVersions
            .FirstOrDefaultAsync(
                v => v.PromptId == prompt.Id && v.Version == request.Version, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Version {request.Version} of prompt '{request.PromptKey}' not found.");

        // Snapshot current state before restoring
        var snapshot = AiPromptVersion.Create(
            prompt.Id,
            prompt.Version,
            prompt.SystemPrompt,
            prompt.UserPromptTemplate,
            prompt.PrimaryProvider,
            prompt.PrimaryModel,
            prompt.FallbackProvider,
            prompt.FallbackModel,
            prompt.Temperature,
            prompt.MaxTokens,
            $"Auto-snapshot before restoring v{request.Version}",
            _currentUser.UserId);

        _db.AiPromptVersions.Add(snapshot);

        prompt.Restore(
            oldVersion.SystemPrompt,
            oldVersion.UserPromptTemplate,
            oldVersion.PrimaryProvider,
            oldVersion.PrimaryModel,
            oldVersion.FallbackProvider,
            oldVersion.FallbackModel,
            oldVersion.Temperature,
            oldVersion.MaxTokens,
            _currentUser.UserId);

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"ai:prompt:{request.PromptKey}");

        return Unit.Value;
    }
}

