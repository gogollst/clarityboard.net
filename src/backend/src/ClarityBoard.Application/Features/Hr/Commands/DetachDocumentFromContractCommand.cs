using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record DetachDocumentFromContractCommand : IRequest<Unit>
{
    public required Guid EmployeeId { get; init; }
    public required Guid ContractId { get; init; }
    public required Guid DocumentId { get; init; }
}

public class DetachDocumentFromContractCommandHandler : IRequestHandler<DetachDocumentFromContractCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DetachDocumentFromContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DetachDocumentFromContractCommand request, CancellationToken cancellationToken)
    {
        var document = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId
                                   && d.EmployeeId == request.EmployeeId
                                   && d.ContractId == request.ContractId, cancellationToken)
            ?? throw new NotFoundException("Document", request.DocumentId);

        document.UnlinkFromContract();
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
