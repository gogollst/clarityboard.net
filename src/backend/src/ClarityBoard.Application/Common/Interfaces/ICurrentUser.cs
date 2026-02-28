namespace ClarityBoard.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid EntityId { get; }
    string Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
}
