using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DTOs.Streak;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositoryLayer.Streak;

namespace ServiceLayer.Streak
{
    public class StreakService : IStreakService
    {
        private readonly LuminaSystemContext _context;
        private readonly ILogger<StreakService> _logger;
        private readonly IStreakRepository _streakRepository;

        // Múi giờ GMT+7 (Việt Nam)
        private static readonly TimeZoneInfo GMT7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        // Danh sách milestone (có thể config từ DB sau)
        private static readonly int[] MILESTONES = { 3, 7, 14, 30, 60, 100, 180, 365 };

        public StreakService(LuminaSystemContext context, ILogger<StreakService> logger, IStreakRepository streakRepository)
        {
            _context = context;
            _logger = logger;
            _streakRepository = streakRepository;
        }

        #region Public Methods

        public DateOnly GetTodayGMT7()
        {
            var nowUtc = DateTime.UtcNow;
            var nowGmt7 = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, GMT7);
            return DateOnly.FromDateTime(nowGmt7);
        }

        public async Task<StreakSummaryDTO> GetStreakSummaryAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new
                    {
                        u.CurrentStreak,
                        u.LongestStreak,
                        u.LastPracticeDate,
                        u.StreakFreezesAvailable
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("GetStreakSummary: User {UserId} not found", userId);
                    return new StreakSummaryDTO();
                }

                var todayLocal = GetTodayGMT7();

                // Convert DateTime? → DateOnly
                DateOnly? lastPracticeDateOnly = user.LastPracticeDate.HasValue 
                    ? DateOnly.FromDateTime(user.LastPracticeDate.Value) 
                    : null;

                var todayCompleted = lastPracticeDateOnly.HasValue 
                    && lastPracticeDateOnly.Value == todayLocal;

                // Handle NULL với ?? 0
                var currentStreak = user.CurrentStreak ?? 0;
                var lastMilestone = GetLastMilestone(currentStreak);
                var nextMilestone = GetNextMilestone(currentStreak);

                return new StreakSummaryDTO
                {
                    CurrentStreak = currentStreak, 
                    LongestStreak = user.LongestStreak ?? 0,  
                    TodayCompleted = todayCompleted,
                    FreezeTokens = user.StreakFreezesAvailable ?? 0,  
                    LastMilestone = lastMilestone,
                    LastPracticeDate = lastPracticeDateOnly,
                    NextMilestone = nextMilestone,
                    DaysToNextMilestone = nextMilestone.HasValue 
                        ? nextMilestone.Value - currentStreak 
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streak summary for user {UserId}", userId);
                throw;
            }
        }

        public async Task<StreakUpdateResultDTO> UpdateOnValidPracticeAsync(int userId, DateOnly practiceDateLocal)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return new StreakUpdateResultDTO
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var todayLocal = GetTodayGMT7();

                if (practiceDateLocal > todayLocal)
                {
                    _logger.LogWarning("Invalid practiceDate {Date} for user {UserId} (future date)", 
                        practiceDateLocal, userId);
                    return new StreakUpdateResultDTO
                    {
                        Success = false,
                        Message = "Invalid practice date (future)"
                    };
                }

                if (practiceDateLocal < todayLocal.AddDays(-2))
                {
                    _logger.LogWarning("Invalid practiceDate {Date} for user {UserId} (too old)", 
                        practiceDateLocal, userId);
                    return new StreakUpdateResultDTO
                    {
                        Success = false,
                        Message = "Invalid practice date (too old)"
                    };
                }

                DateOnly? lastPracticeDateOnly = user.LastPracticeDate.HasValue 
                    ? DateOnly.FromDateTime(user.LastPracticeDate.Value) 
                    : null;

                var eventType = StreakEventType.MaintainDay;
                var message = "";

                // Case 1: Chưa có LastPracticeDate
                if (!lastPracticeDateOnly.HasValue)
                {
                    user.CurrentStreak = 1;
                    user.LongestStreak = 1;
                    user.LastPracticeDate = practiceDateLocal.ToDateTime(TimeOnly.MinValue);
                    user.StreakFreezesAvailable = (user.StreakFreezesAvailable ?? 0) + 1;
                    eventType = StreakEventType.CompleteDay;
                    message = "Bắt đầu chuỗi học tập!";
                }
                // Case 2: Học lại trong cùng ngày
                else if (practiceDateLocal == lastPracticeDateOnly.Value)
                {
                    eventType = StreakEventType.MaintainDay;
                    message = "Bạn đã hoàn thành mục tiêu hôm nay rồi!";
                }
                // Case 3: Ngày kế tiếp (streak +1)
                else if (practiceDateLocal == lastPracticeDateOnly.Value.AddDays(1))
                {
                    user.CurrentStreak = (user.CurrentStreak ?? 0) + 1;
                    user.LastPracticeDate = practiceDateLocal.ToDateTime(TimeOnly.MinValue);
                    eventType = StreakEventType.CompleteDay;
                    message = $"Tuyệt vời! Chuỗi {user.CurrentStreak} ngày 🔥";
                }
                // Case 4: Học lại sau nhiều ngày, nhưng streak vẫn còn (được bảo vệ bởi freeze)
                else if ((user.CurrentStreak ?? 0) > 0 && practiceDateLocal > lastPracticeDateOnly.Value)
                {
                    user.CurrentStreak = (user.CurrentStreak ?? 0) + 1;
                    user.LastPracticeDate = practiceDateLocal.ToDateTime(TimeOnly.MinValue);
                    eventType = StreakEventType.CompleteDay;
                    message = $"Tuyệt vời! Chuỗi {user.CurrentStreak} ngày 🔥";
                }
                // Nếu streak đã bị reset về 0 (do không còn freeze), thì bắt đầu lại từ 1
                else if ((user.CurrentStreak ?? 0) == 0)
                {
                    user.CurrentStreak = 1;
                    user.LongestStreak = Math.Max(user.LongestStreak ?? 0, 1);
                    user.LastPracticeDate = practiceDateLocal.ToDateTime(TimeOnly.MinValue);
                    eventType = StreakEventType.ResetStreak;
                    message = "Chuỗi đã bị ngắt, bắt đầu lại từ đầu!";
                }

                var currentStreak = user.CurrentStreak ?? 0;
                var longestStreak = user.LongestStreak ?? 0;
                if (currentStreak > longestStreak)
                {
                    user.LongestStreak = user.CurrentStreak;
                }

                await _context.SaveChangesAsync();

                int? milestoneReached = null;
                if (eventType == StreakEventType.CompleteDay)
                {
                    milestoneReached = await CheckAndAwardMilestoneAsync(userId, currentStreak);
                }

                var summary = await GetStreakSummaryAsync(userId);

                return new StreakUpdateResultDTO
                {
                    Success = true,
                    Summary = summary,
                    EventType = eventType,
                    MilestoneReached = milestoneReached.HasValue,
                    MilestoneValue = milestoneReached,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak for user {UserId}", userId);
                return new StreakUpdateResultDTO
                {
                    Success = false,
                    Message = "Internal error"
                };
            }
        }

        public async Task<StreakUpdateResultDTO> ApplyAutoFreezeOrResetAsync(int userId, DateOnly todayLocal)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return new StreakUpdateResultDTO { Success = false, Message = "User not found" };
                }

                // ✅ FIX: Chỉ xử lý nếu CurrentStreak > 0 (handle NULL)
                var currentStreak = user.CurrentStreak ?? 0;
                if (currentStreak == 0)
                {
                    return new StreakUpdateResultDTO
                    {
                        Success = true,
                        Message = "No active streak to process"
                    };
                }

                if (!user.LastPracticeDate.HasValue)
                {
                    return new StreakUpdateResultDTO { Success = true, Message = "No practice date" };
                }

                // ✅ FIX: Convert DateTime → DateOnly
                DateOnly lastPracticeDateOnly = DateOnly.FromDateTime(user.LastPracticeDate.Value);

                // Kiểm tra: nếu hôm nay > lastPracticeDate + 1 → lỡ ngày
                if (todayLocal <= lastPracticeDateOnly.AddDays(1))
                {
                    // Chưa lỡ ngày, không cần xử lý
                    return new StreakUpdateResultDTO
                    {
                        Success = true,
                        Message = "Streak is safe"
                    };
                }

                var eventType = StreakEventType.StreakLost;
                var message = "";

                // ✅ FIX: Nếu có Freeze Token → dùng (handle NULL)
                var freezeTokens = user.StreakFreezesAvailable ?? 0;
                if (freezeTokens > 0)
                {
                    user.StreakFreezesAvailable = freezeTokens - 1;
                    eventType = StreakEventType.FreezeUsed;
                    message = $"Freeze token đã được sử dụng! Chuỗi {currentStreak} ngày được bảo vệ.";
                    
                    _logger.LogInformation("Auto-Freeze applied for user {UserId}. Freeze tokens left: {Tokens}",
                        userId, user.StreakFreezesAvailable);
                }
                else
                {
                    // Không có Freeze → Reset về 0
                    user.CurrentStreak = 0;
                    eventType = StreakEventType.StreakLost;
                    message = "Chuỗi học tập đã bị mất. Hãy bắt đầu lại ngay hôm nay!";
                    
                    _logger.LogInformation("Streak lost for user {UserId}. Reset to 0", userId);
                }

                await _context.SaveChangesAsync();

                var summary = await GetStreakSummaryAsync(userId);

                return new StreakUpdateResultDTO
                {
                    Success = true,
                    Summary = summary,
                    EventType = eventType,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying auto-freeze for user {UserId}", userId);
                return new StreakUpdateResultDTO { Success = false, Message = "Internal error" };
            }
        }

        public async Task<int?> CheckAndAwardMilestoneAsync(int userId, int currentStreak)
        {
            try
            {
                // Kiểm tra xem currentStreak có trùng với milestone nào không
                if (!MILESTONES.Contains(currentStreak))
                {
                    return null;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null) return null;

                _logger.LogInformation("User {UserId} reached milestone {Milestone}", userId, currentStreak);

                // Trao thưởng theo milestone
                await AwardMilestoneRewardsAsync(userId, currentStreak);

                return currentStreak;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking milestone for user {UserId}", userId);
                return null;
            }
        }

        public async Task<List<int>> GetUsersNeedingAutoProcessAsync(DateOnly todayLocal)
        {
            try
            {
                
                var yesterdayDateTime = todayLocal.AddDays(-1).ToDateTime(TimeOnly.MaxValue);

                // Lấy users có CurrentStreak > 0 và LastPracticeDate < todayLocal - 1
                var userIds = await _context.Users
                    .Where(u => (u.CurrentStreak ?? 0) > 0  
                        && u.LastPracticeDate.HasValue 
                        && u.LastPracticeDate.Value < yesterdayDateTime)
                    .Select(u => u.UserId)
                    .ToListAsync();

                return userIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users needing auto-process");
                return new List<int>();
            }
        }

        #endregion

        #region Reminder Methods

        /// <summary>
        /// Lấy danh sách users cần nhắc nhở (chưa học hôm nay)
        /// </summary>
        public async Task<List<StreakReminderDTO>> GetUsersNeedingReminderAsync(DateOnly todayLocal)
        {
            var todayDateTime = todayLocal.ToDateTime(TimeOnly.MinValue);

            var users = await _context.Users
                .Where(u => u.RoleId == 4 
                    && (u.CurrentStreak ?? 0) > 0 // Có streak hiện tại
                    && (!u.LastPracticeDate.HasValue 
                        || u.LastPracticeDate.Value < todayDateTime)) // Chưa học hôm nay
                .Select(u => new StreakReminderDTO
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    CurrentStreak = u.CurrentStreak ?? 0,
                    FreezeTokens = u.StreakFreezesAvailable ?? 0,
                    ReminderDate = todayLocal
                })
                .ToListAsync();

            // Tạo message cho từng user
            foreach (var user in users)
            {
                user.ReminderMessage = GenerateReminderMessage(user.CurrentStreak, user.FreezeTokens);
            }

            return users;
        }

        /// <summary>
        /// Tạo nội dung thông báo động viên dựa vào streak hiện tại
        /// </summary>
        public string GenerateReminderMessage(int currentStreak, int freezeTokens)
        {
            // Case 1: Streak cao (>= 30 ngày)
            if (currentStreak >= 30)
            {
                return $"🔥 Chuỗi {currentStreak} ngày của bạn thật ấn tượng! " +
                       $"Đừng để nó bị gián đoạn hôm nay nhé! " +
                       $"({freezeTokens} freeze token còn lại)";
            }

            // Case 2: Streak trung bình (7-29 ngày)
            if (currentStreak >= 7)
            {
                return $"⚡ Bạn đang có chuỗi {currentStreak} ngày liên tiếp! " +
                       $"Hãy duy trì đà tiến bộ này nhé! " +
                       $"({freezeTokens} freeze token còn lại)";
            }

            // Case 3: Streak thấp (1-6 ngày)
            if (currentStreak >= 3)
            {
                return $"💪 Chuỗi {currentStreak} ngày đang được xây dựng! " +
                       $"Hôm nay hãy tiếp tục phát triển nó nhé! " +
                       $"({freezeTokens} freeze token còn lại)";
            }

            // Case 4: Streak mới bắt đầu (1-2 ngày)
            return $"🌟 Bạn đang có chuỗi {currentStreak} ngày! " +
                   $"Đây là khởi đầu tuyệt vời, hãy tiếp tục! " +
                   $"({freezeTokens} freeze token còn lại)";
        }

        #endregion

        #region Private Helpers

        private int? GetLastMilestone(int streak)
        {
            var passed = MILESTONES.Where(m => m <= streak).ToList();
            return passed.Any() ? passed.Max() : null;
        }

        private int? GetNextMilestone(int streak)
        {
            var next = MILESTONES.Where(m => m > streak).ToList();
            return next.Any() ? next.Min() : null;
        }

        private async Task AwardMilestoneRewardsAsync(int userId, int milestone)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return;

            var currentFreezeTokens = user.StreakFreezesAvailable ?? 0;

            // Trao thưởng theo milestone
            switch (milestone)
            {
                case 3:
                    user.StreakFreezesAvailable = currentFreezeTokens + 1;
                    _logger.LogInformation("User {UserId}: +1 Freeze Token (milestone 3)", userId);
                    break;

                case 7:
                    user.StreakFreezesAvailable = currentFreezeTokens + 1;
                    _logger.LogInformation("User {UserId}: +1 Freeze Token (milestone 7)", userId);
                    break;

                case 14:
                    user.StreakFreezesAvailable = currentFreezeTokens + 1;
                    _logger.LogInformation("User {UserId}: +1 Freeze Token (milestone 14)", userId);
                    break;

                case 30:
                    user.StreakFreezesAvailable = currentFreezeTokens + 1;
                    _logger.LogInformation("User {UserId}: +1 Freeze Token (milestone 30)", userId);
                    break;

                case 60:
                    user.StreakFreezesAvailable = currentFreezeTokens + 2;
                    _logger.LogInformation("User {UserId}: +2 Freeze Tokens (milestone 60)", userId);
                    break;

                case 100:
                    user.StreakFreezesAvailable = currentFreezeTokens + 3;
                    _logger.LogInformation("User {UserId}: +3 Freeze Tokens (milestone 100)", userId);
                    break;

                case 180:
                case 365:
                    user.StreakFreezesAvailable = currentFreezeTokens + 5;
                    _logger.LogInformation("User {UserId}: +5 Freeze Tokens (milestone {Milestone})", 
                        userId, milestone);
                    break;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<StreakUserDTO>> GetTopStreakUsersAsync(int topN)
{
    return await _streakRepository.GetTopStreakUsersAsync(topN);
}

        #endregion
    }
}
