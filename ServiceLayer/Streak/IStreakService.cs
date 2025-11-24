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
        /// Lấy thông tin tổng quan về streak của user
        Task<StreakSummaryDTO> GetStreakSummaryAsync(int userId);


        /// Cập nhật streak khi user hoàn thành hoạt động hợp lệ
        Task<StreakUpdateResultDTO> UpdateOnValidPracticeAsync(int userId, DateOnly practiceDateLocal);


        /// Áp dụng Auto-Freeze hoặc Reset khi user lỡ ngày
        Task<StreakUpdateResultDTO> ApplyAutoFreezeOrResetAsync(int userId, DateOnly todayLocal);

        /// Kiểm tra và trao thưởng nếu đạt milestone
        Task<int?> CheckAndAwardMilestoneAsync(int userId, int currentStreak);

        /// Tính toán ngày hôm nay theo GMT+7
        DateOnly GetTodayGMT7();

        /// Lấy danh sách user cần xử lý Auto-Freeze/Reset
        Task<List<int>> GetUsersNeedingAutoProcessAsync(DateOnly todayLocal);

        /// Lấy danh sách users cần nhắc nhở (chưa học hôm nay)
        Task<List<StreakReminderDTO>> GetUsersNeedingReminderAsync(DateOnly todayLocal);

        /// Tạo nội dung thông báo nhắc nhở
        string GenerateReminderMessage(int currentStreak, int freezeTokens);

        /// top10 bxh
        Task<List<StreakUserDTO>> GetTopStreakUsersAsync(int topN);
    }
}
