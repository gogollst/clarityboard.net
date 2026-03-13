using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Commands;

// ── Add ────────────────────────────────────────────────────────────────────────

public record AddProviderModelCommand : IRequest<Guid>
{
    public AiProvider Provider { get; init; }
    public required string ModelId { get; init; }
    public required string DisplayName { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
}

public class AddProviderModelCommandValidator : AbstractValidator<AddProviderModelCommand>
{
    public AddProviderModelCommandValidator()
    {
        RuleFor(x => x.ModelId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class AddProviderModelCommandHandler : IRequestHandler<AddProviderModelCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AddProviderModelCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddProviderModelCommand request, CancellationToken ct)
    {
        var exists = await _db.AiProviderModels
            .AnyAsync(m => m.Provider == request.Provider && m.ModelId == request.ModelId, ct);

        if (exists)
            throw new InvalidOperationException(
                $"Model '{request.ModelId}' already exists for provider {request.Provider}.");

        var model = AiProviderModel.Create(
            request.Provider,
            request.ModelId,
            request.DisplayName,
            request.SortOrder,
            request.Description);

        _db.AiProviderModels.Add(model);
        await _db.SaveChangesAsync(ct);

        return model.Id;
    }
}

// ── Update ─────────────────────────────────────────────────────────────────────

public record UpdateProviderModelCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
}

public class UpdateProviderModelCommandValidator : AbstractValidator<UpdateProviderModelCommand>
{
    public UpdateProviderModelCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateProviderModelCommandHandler : IRequestHandler<UpdateProviderModelCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProviderModelCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProviderModelCommand request, CancellationToken ct)
    {
        var model = await _db.AiProviderModels
            .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Provider model '{request.Id}' not found.");

        model.Update(request.DisplayName, request.SortOrder, request.Description);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

// ── Delete ─────────────────────────────────────────────────────────────────────

public record DeleteProviderModelCommand(Guid Id) : IRequest<Unit>;

public class DeleteProviderModelCommandHandler : IRequestHandler<DeleteProviderModelCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeleteProviderModelCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteProviderModelCommand request, CancellationToken ct)
    {
        var model = await _db.AiProviderModels
            .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Provider model '{request.Id}' not found.");

        _db.AiProviderModels.Remove(model);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

// ── Toggle Active ──────────────────────────────────────────────────────────────

public record ToggleProviderModelCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class ToggleProviderModelCommandHandler : IRequestHandler<ToggleProviderModelCommand, Unit>
{
    private readonly IAppDbContext _db;

    public ToggleProviderModelCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ToggleProviderModelCommand request, CancellationToken ct)
    {
        var model = await _db.AiProviderModels
            .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Provider model '{request.Id}' not found.");

        model.SetActive(request.IsActive);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
