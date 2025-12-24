using DataLayer.Models;

namespace RepositoryLayer.Auth;

public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Get the active (unused, non-expired) password reset token for a user
    /// </summary>
    Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId, bool asTracking = true);

    /// <summary>
    /// Get all unused password reset tokens for a user
    /// </summary>
    Task<List<PasswordResetToken>> GetUnusedTokensByUserIdAsync(int userId);

    /// <summary>
    /// Add a new password reset token
    /// </summary>
    Task AddAsync(PasswordResetToken token);

    /// <summary>
    /// Update an existing password reset token
    /// </summary>
    void Update(PasswordResetToken token);

    /// <summary>
    /// Remove a password reset token
    /// </summary>
    void Remove(PasswordResetToken token);

    /// <summary>
    /// Remove multiple password reset tokens
    /// </summary>
    void RemoveRange(IEnumerable<PasswordResetToken> tokens);

    /// <summary>
    /// Get the most recent used token for a user (to check if registration was completed)
    /// </summary>
    Task<PasswordResetToken?> GetUsedTokenByUserIdAsync(int userId);
}
