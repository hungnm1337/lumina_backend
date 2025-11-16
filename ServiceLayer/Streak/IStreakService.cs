using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;

namespace ServiceLayer.Streak
{
    /// <summary>
    /// Service xử lý logic chuỗi học tập (Streak)
    /// </summary>
    public interface IStreakService
    {
        /// <summary>
        /// Lấy thông tin tổng quan về streak của user
        /// </summary>
        /// <param name="userId">ID của user</param>
        /// <returns>StreakSummaryDTO</returns>
        Task<StreakSummaryDTO> GetStreakSummaryAsync(int userId);

        /// <summary>
        /// Cập nhật streak khi user hoàn thành hoạt động hợp lệ
        /// </summary>
        /// <param name="userId">ID của user</param>
        /// <param name="practiceDateLocal">Ngày thực hiện (GMT+7)</param>
        /// <returns>Kết quả update (bao gồm milestone nếu có)</returns>
        Task<StreakUpdateResultDTO> UpdateOnValidPracticeAsync(int userId, DateOnly practiceDateLocal);

        /// <summary>
        /// Áp dụng Auto-Freeze hoặc Reset khi user lỡ ngày
        /// (Gọi bởi Cron Job)
        /// </summary>
        /// <param name="userId">ID của user</param>
        /// <param name="todayLocal">Ngày hôm nay (GMT+7)</param>
        /// <returns>Kết quả (FreezeUsed hoặc StreakLost)</returns>
        Task<StreakUpdateResultDTO> ApplyAutoFreezeOrResetAsync(int userId, DateOnly todayLocal);

        /// <summary>
        /// Kiểm tra và trao thưởng nếu đạt milestone
        /// </summary>
        /// <param name="userId">ID của user</param>
        /// <param name="currentStreak">Chuỗi hiện tại</param>
        /// <returns>Milestone đạt được (nếu có)</returns>
        Task<int?> CheckAndAwardMilestoneAsync(int userId, int currentStreak);

        /// <summary>
        /// Tính toán ngày hôm nay theo GMT+7
        /// </summary>
        /// <returns>DateOnly (GMT+7)</returns>
        DateOnly GetTodayGMT7();

        /// <summary>
        /// Lấy danh sách user cần xử lý Auto-Freeze/Reset
        /// (Dùng cho Cron Job)
        /// </summary>
        /// <param name="todayLocal">Ngày hôm nay (GMT+7)</param>
        /// <returns>Danh sách userId</returns>
        Task<List<int>> GetUsersNeedingAutoProcessAsync(DateOnly todayLocal);

        /// <summary>
        /// Lấy danh sách users cần nhắc nhở (chưa học hôm nay)
        /// </summary>
        Task<List<StreakReminderDTO>> GetUsersNeedingReminderAsync(DateOnly todayLocal);

        /// <summary>
        /// Tạo nội dung thông báo nhắc nhở
        /// </summary>
        string GenerateReminderMessage(int currentStreak, int freezeTokens);
    }
}
