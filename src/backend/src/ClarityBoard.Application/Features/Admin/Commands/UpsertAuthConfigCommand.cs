using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Admin;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

public record UpsertAuthConfigCommand : IRequest
{
    public required int TokenLifetimeHours { get; init; }
    public required int RememberMeTokenLifetimeDays { get; init; }
}

public class UpsertAuthConfigCommandValidator : AbstractValidator<UpsertAuthConfigCommand>
{
    public UpsertAuthConfigCommandValidator()
    {
        RuleFor(x => x.TokenLifetimeHours).InclusiveBetween(1, 168);
        RuleFor(x => x.RememberMeTokenLifetimeDays).InclusiveBetween(1, 365);
    }
}

public class UpsertAuthConfigCommandHandler : IRequestHandler<UpsertAuthConfigCommand>
{
    private readonly IAppDbContext _db;

    public UpsertAuthConfigCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UpsertAuthConfigCommand request, CancellationToken ct)
    {
        var config = await _db.AuthConfigs.FirstOrDefaultAsync(ct);

        if (config is null)
        {
            config = AuthConfig.CreateDefault();
            _db.AuthConfigs.Add(config);
        }

        config.Update(request.TokenLifetimeHours, request.RememberMeTokenLifetimeDays);
        await _db.SaveChangesAsync(ct);
    }
}
