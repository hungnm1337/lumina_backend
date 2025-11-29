using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;

namespace ServiceLayer.Streak
{
    public interface IStreakService
    {
        Task<StreakSummaryDTO> GetStreakSummaryAsync(int userId);

        Task<StreakUpdateResultDTO> UpdateOnValidPracticeAsync(int userId, DateOnly practiceDateLocal);

        Task<StreakUpdateResultDTO> ApplyAutoFreezeOrResetAsync(int userId, DateOnly todayLocal);

        Task<int?> CheckAndAwardMilestoneAsync(int userId, int currentStreak);

        DateOnly GetTodayGMT7();

        Task<List<int>> GetUsersNeedingAutoProcessAsync(DateOnly todayLocal);

        Task<List<StreakReminderDTO>> GetUsersNeedingReminderAsync(DateOnly todayLocal);

        string GenerateReminderMessage(int currentStreak, int freezeTokens);

        /// top10 bxh
        Task<List<StreakUserDTO>> GetTopStreakUsersAsync(int topN);
    }
}
