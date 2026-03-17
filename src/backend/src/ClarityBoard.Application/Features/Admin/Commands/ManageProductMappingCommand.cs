using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

// ── Upsert (Create or Update) ──

public record UpsertProductMappingCommand : IRequest<Guid>
{
    public Guid? Id { get; init; } // null = create, non-null = update
    public required Guid EntityId { get; init; }
    public required string ProductNamePattern { get; init; }
    public required string ProductCategory { get; init; }
    public Guid? RevenueAccountId { get; init; }
    public bool IsActive { get; init; } = true;
}

public class UpsertProductMappingCommandValidator : AbstractValidator<UpsertProductMappingCommand>
{
    public UpsertProductMappingCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.ProductNamePattern).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ProductCategory).NotEmpty().MaximumLength(50);
    }
}

public class UpsertProductMappingCommandHandler : IRequestHandler<UpsertProductMappingCommand, Guid>
{
    private readonly IAppDbContext _db;

    public UpsertProductMappingCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertProductMappingCommand request, CancellationToken ct)
    {
        if (request.Id.HasValue)
        {
            var existing = await _db.ProductCategoryMappings
                .FirstOrDefaultAsync(m => m.Id == request.Id.Value && m.EntityId == request.EntityId, ct)
                ?? throw new KeyNotFoundException($"ProductCategoryMapping {request.Id} not found");

            existing.Update(request.ProductNamePattern, request.ProductCategory, request.RevenueAccountId, request.IsActive);
            await _db.SaveChangesAsync(ct);
            return existing.Id;
        }

        var mapping = ProductCategoryMapping.Create(
            request.EntityId, request.ProductNamePattern, request.ProductCategory, request.RevenueAccountId);
        _db.ProductCategoryMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping.Id;
    }
}

// ── Delete ──

public record DeleteProductMappingCommand(Guid EntityId, Guid MappingId) : IRequest;

public class DeleteProductMappingCommandHandler : IRequestHandler<DeleteProductMappingCommand>
{
    private readonly IAppDbContext _db;

    public DeleteProductMappingCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteProductMappingCommand request, CancellationToken ct)
    {
        var mapping = await _db.ProductCategoryMappings
            .FirstOrDefaultAsync(m => m.Id == request.MappingId && m.EntityId == request.EntityId, ct)
            ?? throw new KeyNotFoundException($"ProductCategoryMapping {request.MappingId} not found");

        _db.ProductCategoryMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
    }
}
