using System.Globalization;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Auth;

public class AccountRepository : IAccountRepository
{
    private const string GoogleProvider = "Google";
    private readonly LuminaSystemContext _context;

    public AccountRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<Account?> FindByIdentifierAsync(string identifier)
    {
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();

        var accountsQuery = _context.Accounts
            .Where(a => a.AuthProvider == null || a.AuthProvider == string.Empty)
            .Include(a => a.User)
                .ThenInclude(u => u.Role);

        // Check if identifier looks like an email
        if (normalizedIdentifier.Contains('@'))
        {
            var account = await accountsQuery
                .FirstOrDefaultAsync(a => a.User.Email == normalizedIdentifier);

            if (account != null)
            {
                return account;
            }
        }

        // Try to find by username
        return await accountsQuery
            .FirstOrDefaultAsync(a => a.Username == normalizedIdentifier);
    }

    public async Task<Account?> GetByGoogleIdWithUserAndRoleAsync(string googleSubject)
    {
        return await _context.Accounts
            .Include(a => a.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(a =>
                a.AuthProvider == GoogleProvider &&
                a.ProviderUserId == googleSubject);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _context.Accounts.AnyAsync(a => a.Username == normalizedUsername);
    }

    public async Task<string> GenerateUniqueUsernameAsync(string baseUsername, int maxLength = 20)
    {
        var normalizedBase = baseUsername.Trim().ToLowerInvariant();

        if (normalizedBase.Length > maxLength - 3)
        {
            normalizedBase = normalizedBase[..(maxLength - 3)];
        }

        var candidate = normalizedBase;
        var suffix = 0;

        while (await _context.Accounts.AnyAsync(a => a.Username == candidate))
        {
            suffix++;
            var suffixStr = suffix.ToString(CultureInfo.InvariantCulture);
            var maxBaseLength = maxLength - suffixStr.Length - 1; // -1 for the dash

            if (normalizedBase.Length > maxBaseLength)
            {
                candidate = $"{normalizedBase[..maxBaseLength]}-{suffixStr}";
            }
            else
            {
                candidate = $"{normalizedBase}-{suffixStr}";
            }
        }

        return candidate;
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public void Update(Account account)
    {
        _context.Accounts.Update(account);
    }
}
