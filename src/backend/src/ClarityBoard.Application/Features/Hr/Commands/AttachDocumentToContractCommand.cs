using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record AttachDocumentToContractCommand : IRequest<Unit>
{
    public required Guid EmployeeId { get; init; }
    public required Guid ContractId { get; init; }
    public required Guid DocumentId { get; init; }
}

public class AttachDocumentToContractCommandHandler : IRequestHandler<AttachDocumentToContractCommand, Unit>
{
    private readonly IAppDbContext _db;

    public AttachDocumentToContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(AttachDocumentToContractCommand request, CancellationToken cancellationToken)
    {
        var contractExists = await _db.Contracts
            .AnyAsync(c => c.Id == request.ContractId && c.EmployeeId == request.EmployeeId, cancellationToken);
        if (!contractExists)
            throw new NotFoundException("Contract", request.ContractId);

        var document = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Document", request.DocumentId);

        document.LinkToContract(request.ContractId);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
