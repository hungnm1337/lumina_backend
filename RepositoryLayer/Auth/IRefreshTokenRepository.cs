using DataLayer.Models;

namespace RepositoryLayer.Auth;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Get refresh token by token string with User, Role, and Accounts included
    /// </summary>
    Task<RefreshToken?> GetByTokenWithUserAndAccountsAsync(string token);

    /// <summary>
    /// Get all active (non-revoked, non-expired) refresh tokens for a user
    /// </summary>
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);

    /// <summary>
    /// Revoke all active refresh tokens for a user
    /// </summary>
    Task RevokeAllActiveByUserIdAsync(int userId, string reason);

    /// <summary>
    /// Add a new refresh token
    /// </summary>
    Task AddAsync(RefreshToken refreshToken);

    /// <summary>
    /// Update an existing refresh token
    /// </summary>
    void Update(RefreshToken refreshToken);
}
