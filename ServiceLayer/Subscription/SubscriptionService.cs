using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Subscription
{
    public interface ISubscriptionService
    {
        Task<DataLayer.Models.Subscription> ActivateSubscriptionAsync(int userId, int packageId, int paymentId);
        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task<DataLayer.Models.Subscription?> GetActiveSubscriptionAsync(int userId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly LuminaSystemContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(LuminaSystemContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DataLayer.Models.Subscription> ActivateSubscriptionAsync(int userId, int packageId, int paymentId)
        {
            try
            {
                var package = await _context.Packages.FindAsync(packageId);
                if (package == null)
                {
                    throw new Exception($"Package {packageId} not found");
                }

                var startTime = DateTime.UtcNow;
                var endTime = startTime.AddDays(package.DurationInDays ?? 30);

                var subscription = new DataLayer.Models.Subscription
                {
                    UserId = userId,
                    PackageId = packageId,
                    PaymentId = paymentId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = "Active"
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription activated for user {UserId}, package {PackageId}", userId, packageId);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating subscription for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            return subscription != null;
        }

        public async Task<DataLayer.Models.Subscription?> GetActiveSubscriptionAsync(int userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId &&
                           s.Status == "Active" &&
                           s.EndTime.HasValue &&
                           s.EndTime.Value > DateTime.UtcNow)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefaultAsync();
        }
    }
}
