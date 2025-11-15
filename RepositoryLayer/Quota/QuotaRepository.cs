using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Quota
{
    public class QuotaRepository : IQuotaRepository
    {
        private readonly LuminaSystemContext _context;

        public QuotaRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<UserQuotaStatus> GetUserQuotaStatusAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found");
            }

            // Check if user has active premium subscription
            var hasActiveSubscription = user.Subscriptions.Any(s =>
                s.Status == "Active" &&
                s.EndTime.HasValue &&
                s.EndTime.Value > DateTime.UtcNow);

            return new UserQuotaStatus
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                SubscriptionType = hasActiveSubscription ? "PREMIUM" : "FREE",
                MonthlyReadingAttempts = user.MonthlyReadingAttempts,
                MonthlyListeningAttempts = user.MonthlyListeningAttempts,
                LastQuotaReset = user.LastQuotaReset,
                HasActiveSubscription = hasActiveSubscription
            };
        }

        public async Task IncrementAttemptAsync(int userId, string skill)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found");
            }

            // Check and reset quota if new month
            await CheckAndResetQuotaAsync(userId);

            // Increment the appropriate counter
            switch (skill.ToLower())
            {
                case "reading":
                    user.MonthlyReadingAttempts++;
                    break;
                case "listening":
                    user.MonthlyListeningAttempts++;
                    break;
                // Speaking and Writing don't have quotas for free tier (blocked entirely)
                case "speaking":
                case "writing":
                    // Only increment for premium users (they have unlimited)
                    break;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CheckAndResetQuotaAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var now = DateTime.UtcNow;
            var lastReset = user.LastQuotaReset;

            // Check if it's a new month
            if (lastReset.Year != now.Year || lastReset.Month != now.Month)
            {
                user.MonthlyReadingAttempts = 0;
                user.MonthlyListeningAttempts = 0;
                user.LastQuotaReset = now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResetAllQuotasAsync()
        {
            var users = await _context.Users.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var user in users)
            {
                user.MonthlyReadingAttempts = 0;
                user.MonthlyListeningAttempts = 0;
                user.LastQuotaReset = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
