using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Auth;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LuminaSystemContext _context;

    public RefreshTokenRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenWithUserAndAccountsAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
            .Include(rt => rt.User.Accounts)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId
                      && !rt.IsRevoked
                      && rt.ExpiresAt > now)
            .ToListAsync();
    }

    public async Task RevokeAllActiveByUserIdAsync(int userId, string reason)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId
                      && !rt.IsRevoked
                      && rt.ExpiresAt > now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
        }
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }
}
