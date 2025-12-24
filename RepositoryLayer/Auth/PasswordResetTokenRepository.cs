using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Auth;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly LuminaSystemContext _context;

    public PasswordResetTokenRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId, bool asTracking = true)
    {
        IQueryable<PasswordResetToken> query = _context.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(token => token.CreatedAt);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<PasswordResetToken>> GetUnusedTokensByUserIdAsync(int userId)
    {
        return await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync();
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _context.PasswordResetTokens.AddAsync(token);
    }

    public void Update(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Update(token);
    }

    public void Remove(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Remove(token);
    }

    public void RemoveRange(IEnumerable<PasswordResetToken> tokens)
    {
        _context.PasswordResetTokens.RemoveRange(tokens);
    }

    public async Task<PasswordResetToken?> GetUsedTokenByUserIdAsync(int userId)
    {
        return await _context.PasswordResetTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId && token.UsedAt != null)
            .OrderByDescending(token => token.UsedAt)
            .FirstOrDefaultAsync();
    }
}
