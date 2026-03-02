namespace ClarityBoard.Application.Features.UserProfile.DTOs;

public record UserProfileResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Locale { get; init; } = default!;
    public string Timezone { get; init; } = default!;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
