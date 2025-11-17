using RepositoryLayer.Quota;

namespace ServiceLayer.Quota
{
    public interface IQuotaService
    {
        Task<QuotaCheckResult> CheckQuotaAsync(int userId, string skill);
        Task IncrementQuotaAsync(int userId, string skill);
        Task ResetMonthlyQuotaAsync();
    }

    public class QuotaService : IQuotaService
    {
        private readonly IQuotaRepository _quotaRepo;
        private const int FREE_TIER_LIMIT = 20;

        public QuotaService(IQuotaRepository quotaRepo)
        {
            _quotaRepo = quotaRepo;
        }

        public async Task<QuotaCheckResult> CheckQuotaAsync(int userId, string skill)
        {
            var userStatus = await _quotaRepo.GetUserQuotaStatusAsync(userId);

            // Premium users: unlimited access to all skills
            if (userStatus.SubscriptionType == "PREMIUM")
            {
                return new QuotaCheckResult
                {
                    CanAccess = true,
                    IsPremium = true,
                    RequiresUpgrade = false,
                    RemainingAttempts = -1, // -1 indicates unlimited
                    SubscriptionType = "PREMIUM"
                };
            }

            // Free users: check skill-specific rules
            return skill.ToLower() switch
            {
                "reading" => new QuotaCheckResult
                {
                    CanAccess = userStatus.MonthlyReadingAttempts < FREE_TIER_LIMIT,
                    IsPremium = false,
                    RequiresUpgrade = false,
                    RemainingAttempts = Math.Max(0, FREE_TIER_LIMIT - userStatus.MonthlyReadingAttempts),
                    SubscriptionType = "FREE"
                },
                "listening" => new QuotaCheckResult
                {
                    CanAccess = userStatus.MonthlyListeningAttempts < FREE_TIER_LIMIT,
                    IsPremium = false,
                    RequiresUpgrade = false,
                    RemainingAttempts = Math.Max(0, FREE_TIER_LIMIT - userStatus.MonthlyListeningAttempts),
                    SubscriptionType = "FREE"
                },
                // Speaking and Writing require premium
                "speaking" or "writing" => new QuotaCheckResult
                {
                    CanAccess = false,
                    IsPremium = false,
                    RequiresUpgrade = true,
                    RemainingAttempts = 0,
                    SubscriptionType = "FREE"
                },
                _ => new QuotaCheckResult
                {
                    CanAccess = false,
                    IsPremium = false,
                    RequiresUpgrade = false,
                    RemainingAttempts = 0,
                    SubscriptionType = "FREE"
                }
            };
        }

        public async Task IncrementQuotaAsync(int userId, string skill)
        {
            // Check and reset quota if it's a new month
            await _quotaRepo.CheckAndResetQuotaAsync(userId);

            // Increment the attempt counter
            await _quotaRepo.IncrementAttemptAsync(userId, skill);
        }

        public async Task ResetMonthlyQuotaAsync()
        {
            // This will be called by a scheduled job (Hangfire) at the start of each month
            await _quotaRepo.ResetAllQuotasAsync();
        }
    }
}
