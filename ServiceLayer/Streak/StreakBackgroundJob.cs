using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Streak
{
    public class StreakBackgroundJob
    {
        private readonly IStreakService _streakService;
        private readonly ILogger<StreakBackgroundJob> _logger;

        public StreakBackgroundJob(
            IStreakService streakService,
            ILogger<StreakBackgroundJob> logger)
        {
            _streakService = streakService;
            _logger = logger;
        }

        
        public async Task ProcessDailyStreaksAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("=== START Daily Streak Processing at {Time} UTC ===", startTime);

            try
            {
                // 1. Lấy ngày hiện tại theo GMT+7
                var todayLocal = _streakService.GetTodayGMT7();
                _logger.LogInformation("Processing streaks for date: {Date} GMT+7", todayLocal);

                // 2. Lấy danh sách users cần xử lý (CurrentStreak > 0 và LastPracticeDate < yesterday)
                var userIds = await _streakService.GetUsersNeedingAutoProcessAsync(todayLocal);
                
                if (!userIds.Any())
                {
                    _logger.LogInformation("No users need streak processing today");
                    return;
                }

                _logger.LogInformation("Found {Count} users needing streak processing", userIds.Count);

                // 3. Xử lý từng user
                int successCount = 0;
                int freezeUsedCount = 0;
                int streakLostCount = 0;
                int errorCount = 0;

                foreach (var userId in userIds)
                {
                    try
                    {
                        var result = await _streakService.ApplyAutoFreezeOrResetAsync(userId, todayLocal);

                        if (result.Success)
                        {
                            successCount++;

                            if (result.EventType == StreakEventType.FreezeUsed)
                            {
                                freezeUsedCount++;
                                _logger.LogInformation(
                                    "User {UserId}: Freeze token used. Message: {Message}", 
                                    userId, result.Message);
                            }
                            else if (result.EventType == StreakEventType.StreakLost)
                            {
                                streakLostCount++;
                                _logger.LogWarning(
                                    "User {UserId}: Streak lost. Message: {Message}", 
                                    userId, result.Message);
                            }
                        }
                        else
                        {
                            errorCount++;
                            _logger.LogError(
                                "Failed to process streak for user {UserId}: {Message}", 
                                userId, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, 
                            "Exception while processing streak for user {UserId}", userId);
                    }

                    // Delay nhỏ để tránh overload DB (optional)
                    await Task.Delay(50);
                }

                // 4. Log summary
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "=== COMPLETED Daily Streak Processing ===\n" +
                    "Duration: {Duration}ms\n" +
                    "Total Users: {Total}\n" +
                    "Success: {Success}\n" +
                    "Freeze Used: {FreezeUsed}\n" +
                    "Streak Lost: {StreakLost}\n" +
                    "Errors: {Errors}",
                    duration.TotalMilliseconds,
                    userIds.Count,
                    successCount,
                    freezeUsedCount,
                    streakLostCount,
                    errorCount
                );
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, 
                    "FATAL ERROR in Daily Streak Processing after {Duration}ms", 
                    duration.TotalMilliseconds);
                throw; // Re-throw để Hangfire retry
            }
        }
    }
}
