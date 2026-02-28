using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.close_period")]
public record CloseFiscalPeriodCommand(Guid EntityId, Guid PeriodId, bool HardClose = false) : ICommand, IEntityScoped;

public class CloseFiscalPeriodCommandHandler : IRequestHandler<CloseFiscalPeriodCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CloseFiscalPeriodCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseFiscalPeriodCommand request, CancellationToken ct)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Fiscal period '{request.PeriodId}' not found.");

        if (request.HardClose)
            period.HardClose(_currentUser.UserId);
        else
            period.SoftClose(_currentUser.UserId);

        await _db.SaveChangesAsync(ct);
    }
}

[RequirePermission("accounting.reopen_period")]
public record ReopenFiscalPeriodCommand(Guid EntityId, Guid PeriodId) : ICommand, IEntityScoped;

public class ReopenFiscalPeriodCommandHandler : IRequestHandler<ReopenFiscalPeriodCommand>
{
    private readonly IAppDbContext _db;

    public ReopenFiscalPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ReopenFiscalPeriodCommand request, CancellationToken ct)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Fiscal period '{request.PeriodId}' not found.");

        period.Reopen();
        await _db.SaveChangesAsync(ct);
    }
}
