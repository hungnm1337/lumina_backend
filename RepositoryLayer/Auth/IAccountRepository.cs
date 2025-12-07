using DataLayer.Models;

namespace RepositoryLayer.Auth;

public interface IAccountRepository
{
    /// <summary>
    /// Find account by username or email (for local login)
    /// </summary>
    Task<Account?> FindByIdentifierAsync(string identifier);

    /// <summary>
    /// Get account by Google Subject ID with User and Role included
    /// </summary>
    Task<Account?> GetByGoogleIdWithUserAndRoleAsync(string googleSubject);

    /// <summary>
    /// Check if username already exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Generate a unique username based on base username
    /// </summary>
    Task<string> GenerateUniqueUsernameAsync(string baseUsername, int maxLength = 20);

    /// <summary>
    /// Add a new account
    /// </summary>
    Task AddAsync(Account account);

    /// <summary>
    /// Update an existing account
    /// </summary>
    void Update(Account account);
}
