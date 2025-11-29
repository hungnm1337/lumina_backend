using RepositoryLayer.Quota;
using DataLayer.DTOs.Quota;

namespace ServiceLayer.Quota
{
    public interface IQuotaService
    {
        Task<QuotaCheckResult> CheckQuotaAsync(int userId, string skill);
        Task IncrementQuotaAsync(int userId, string skill);
        Task ResetMonthlyQuotaAsync();
        Task<QuotaRemainingDto> GetRemainingQuotaAsync(int userId);
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

            if (userStatus.SubscriptionType == "PREMIUM")
            {
                return new QuotaCheckResult
                {
                    CanAccess = true,
                    IsPremium = true,
                    RequiresUpgrade = false,
                    RemainingAttempts = -1, 
                    SubscriptionType = "PREMIUM"
                };
            }

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
            await _quotaRepo.CheckAndResetQuotaAsync(userId);

            await _quotaRepo.IncrementAttemptAsync(userId, skill);
        }

        public async Task ResetMonthlyQuotaAsync()
        {
            await _quotaRepo.ResetAllQuotasAsync();
        }

        public async Task<QuotaRemainingDto> GetRemainingQuotaAsync(int userId)
        {
            var userStatus = await _quotaRepo.GetUserQuotaStatusAsync(userId);

            if (userStatus.SubscriptionType == "PREMIUM")
            {
                return new QuotaRemainingDto
                {
                    IsPremium = true,
                    ReadingRemaining = -1, // Unlimited
                    ListeningRemaining = -1,
                    ReadingUsed = userStatus.MonthlyReadingAttempts,
                    ListeningUsed = userStatus.MonthlyListeningAttempts,
                    ReadingLimit = -1,
                    ListeningLimit = -1
                };
            }

            return new QuotaRemainingDto
            {
                IsPremium = false,
                ReadingRemaining = Math.Max(0, FREE_TIER_LIMIT - userStatus.MonthlyReadingAttempts),
                ListeningRemaining = Math.Max(0, FREE_TIER_LIMIT - userStatus.MonthlyListeningAttempts),
                ReadingUsed = userStatus.MonthlyReadingAttempts,
                ListeningUsed = userStatus.MonthlyListeningAttempts,
                ReadingLimit = FREE_TIER_LIMIT,
                ListeningLimit = FREE_TIER_LIMIT
            };
        }
    }
}
