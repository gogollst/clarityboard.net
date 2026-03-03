namespace ClarityBoard.Domain.Entities.Identity;

public enum UserStatus { Active = 0, Invited = 1, Inactive = 2 }

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string? PasswordHash { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Locale { get; private set; } = "de";
    public string Timezone { get; private set; } = "Europe/Berlin";
    public string? AvatarPath { get; private set; }
    public string? Bio { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; } // Encrypted TOTP secret
    public string? RecoveryCodesHash { get; private set; } // JSON array of hashed recovery codes
    public bool IsActive { get; private set; } = true;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string? InvitationToken { get; private set; }
    public DateTime? InvitationTokenExpiry { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string? passwordHash, string firstName, string lastName)
    {
        var status = passwordHash != null ? UserStatus.Active : UserStatus.Invited;
        return new User
        {
            Id           = Guid.NewGuid(),
            Email        = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Status       = status,
            IsActive     = status == UserStatus.Active,
            FirstName    = firstName,
            LastName     = lastName,
            CreatedAt    = DateTime.UtcNow,
        };
    }

    public string FullName => $"{FirstName} {LastName}";

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockedUntil = DateTime.UtcNow.AddMinutes(FailedLoginAttempts * 5);
    }

    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    public void Setup2FA(string secret, string recoveryCodesHash)
    {
        TwoFactorSecret = secret;
        RecoveryCodesHash = recoveryCodesHash;
    }

    public void Enable2FA(string secret)
    {
        TwoFactorSecret = secret;
        TwoFactorEnabled = true;
    }

    public void Confirm2FA()
    {
        TwoFactorEnabled = true;
    }

    public void Disable2FA()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
        RecoveryCodesHash = null;
    }

    public void UpdateRecoveryCodes(string recoveryCodesHash)
    {
        RecoveryCodesHash = recoveryCodesHash;
    }

    public void Deactivate()
    {
        IsActive  = false;
        Status    = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive  = true;
        Status    = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string locale, string timezone)
    {
        FirstName = firstName;
        LastName  = lastName;
        Locale    = locale;
        Timezone  = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName  = lastName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt    = DateTime.UtcNow;
    }

    public void UpdateBio(string? bio)
    {
        Bio       = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvatarPath(string? avatarPath)
    {
        AvatarPath = avatarPath;
        UpdatedAt  = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string token, DateTime expiry)
    {
        PasswordResetToken       = token;
        PasswordResetTokenExpiry = expiry;
        UpdatedAt                = DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken       = null;
        PasswordResetTokenExpiry = null;
        UpdatedAt                = DateTime.UtcNow;
    }

    public void SetInvitationToken(string token, DateTime expiry)
    {
        InvitationToken       = token;
        InvitationTokenExpiry = expiry;
        UpdatedAt             = DateTime.UtcNow;
    }

    public void ClearInvitationToken()
    {
        InvitationToken       = null;
        InvitationTokenExpiry = null;
        UpdatedAt             = DateTime.UtcNow;
    }

    public void AcceptInvitation(string passwordHash)
    {
        PasswordHash = passwordHash;
        Status       = UserStatus.Active;
        IsActive     = true;
        ClearInvitationToken();
        UpdatedAt = DateTime.UtcNow;
    }
}
