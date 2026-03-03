namespace ClarityBoard.Application.Features.Admin.DTOs;

public record UserListDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required bool IsActive { get; init; }
    public required string Status { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<UserRoleDto> Roles { get; init; }
}

public record UserDetailDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Locale { get; init; }
    public required string Timezone { get; init; }
    public required bool IsActive { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public required int FailedLoginAttempts { get; init; }
    public DateTime? LockedUntil { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required IReadOnlyList<UserRoleDto> Roles { get; init; }
    public required IReadOnlyList<UserEntityAccessDto> EntityAccess { get; init; }
    public required IReadOnlyList<AuditLogDto> RecentAuditLogs { get; init; }
}

public record UserRoleDto
{
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public required Guid EntityId { get; init; }
    public required string EntityName { get; init; }
    public required DateTime AssignedAt { get; init; }
}

public record UserEntityAccessDto
{
    public required Guid EntityId { get; init; }
    public required string EntityName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}

public record AuditLogDto
{
    public required Guid Id { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public required string Action { get; init; }
    public required string TableName { get; init; }
    public string? RecordId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record CreateUserResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
