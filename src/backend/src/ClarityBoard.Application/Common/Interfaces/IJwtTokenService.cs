namespace ClarityBoard.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, Guid entityId, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    (Guid userId, string email)? ValidateAccessToken(string token);
}
