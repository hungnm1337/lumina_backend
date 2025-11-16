using DataLayer.Models;

namespace RepositoryLayer.Quota
{
    public class QuotaCheckResult
    {
        public bool CanAccess { get; set; }
        public bool IsPremium { get; set; }
        public bool RequiresUpgrade { get; set; }
        public int RemainingAttempts { get; set; }
        public string SubscriptionType { get; set; } = "FREE";
    }

    public class UserQuotaStatus
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string SubscriptionType { get; set; } = "FREE";
        public int MonthlyReadingAttempts { get; set; }
        public int MonthlyListeningAttempts { get; set; }
        public DateTime LastQuotaReset { get; set; }
        public bool HasActiveSubscription { get; set; }
    }

    public interface IQuotaRepository
    {
        Task<UserQuotaStatus> GetUserQuotaStatusAsync(int userId);
        Task IncrementAttemptAsync(int userId, string skill);
        Task CheckAndResetQuotaAsync(int userId);
        Task ResetAllQuotasAsync();
    }
}
