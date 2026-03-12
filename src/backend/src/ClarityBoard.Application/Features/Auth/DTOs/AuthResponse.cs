namespace ClarityBoard.Application.Features.Auth.DTOs;

public record AuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required UserInfo User { get; init; }
    public bool RequiresTwoFactor { get; init; }
}

public record UserInfo
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Locale { get; init; }
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<EntityAccess> Entities { get; init; } = [];
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public record EntityAccess
{
    public required Guid EntityId { get; init; }
    public required string EntityName { get; init; }
    public required string Role { get; init; }
}
