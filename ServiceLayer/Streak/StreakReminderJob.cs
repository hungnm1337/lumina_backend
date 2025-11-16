using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceLayer.Email;
using DataLayer.DTOs.Streak;

namespace ServiceLayer.Streak
{
    /// <summary>
    /// Background job gửi nhắc nhở streak hàng ngày lúc 21:00 GMT+7
    /// </summary>
    public class StreakReminderJob
    {
        private readonly IStreakService _streakService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<StreakReminderJob> _logger;

        public StreakReminderJob(
            IStreakService streakService,
            IEmailSender emailSender,
            ILogger<StreakReminderJob> logger)
        {
            _streakService = streakService;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Job chạy hàng ngày lúc 21:00 GMT+7
        /// Gửi email nhắc nhở cho users chưa học hôm nay
        /// </summary>
        public async Task ProcessDailyRemindersAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("=== START Daily Streak Reminder at {Time} UTC ===", startTime);

            try
            {
                // 1. Lấy ngày hiện tại theo GMT+7
                var todayLocal = _streakService.GetTodayGMT7();
                _logger.LogInformation("Processing reminders for date: {Date} GMT+7", todayLocal);

                // 2. Lấy danh sách users cần nhắc nhở
                var usersToRemind = await _streakService.GetUsersNeedingReminderAsync(todayLocal);

                if (!usersToRemind.Any())
                {
                    _logger.LogInformation("No users need reminder today - All users have practiced!");
                    return;
                }

                _logger.LogInformation("Found {Count} users needing reminder", usersToRemind.Count);

                // 3. Gửi email cho từng user
                int successCount = 0;
                int errorCount = 0;

                foreach (var user in usersToRemind)
                {
                    try
                    {
                        await SendReminderEmailAsync(user);
                        successCount++;

                        _logger.LogInformation(
                            "Reminder sent to user {UserId} ({Email}): Streak {Streak} days",
                            user.UserId,
                            user.Email,
                            user.CurrentStreak);

                        // Delay nhỏ để tránh spam email server
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex,
                            "Failed to send reminder to user {UserId} ({Email})",
                            user.UserId,
                            user.Email);
                    }
                }

                // 4. Log summary
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "=== COMPLETED Daily Streak Reminder ===\n" +
                    "Duration: {Duration}ms\n" +
                    "Total Users: {Total}\n" +
                    "Emails Sent: {Sent}\n" +
                    "Errors: {Errors}\n" +
                    "Success Rate: {Rate:P2}",
                    duration.TotalMilliseconds,
                    usersToRemind.Count,
                    successCount,
                    errorCount,
                    usersToRemind.Count > 0 ? (double)successCount / usersToRemind.Count : 0
                );
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "FATAL ERROR in Daily Streak Reminder after {Duration}ms",
                    duration.TotalMilliseconds);
                throw; // Re-throw để Hangfire retry
            }
        }

        /// <summary>
        /// Gửi email nhắc nhở cho 1 user
        /// </summary>
        private async Task SendReminderEmailAsync(StreakReminderDTO user)
        {
            var subject = $"⏰ Nhắc nhở: Hãy duy trì chuỗi {user.CurrentStreak} ngày của bạn!";

            var body = GenerateEmailBody(user);

            await _emailSender.SendEmailAsync(user.Email, subject, body);
        }

        /// <summary>
        /// Tạo nội dung email HTML
        /// </summary>
        private string GenerateEmailBody(StreakReminderDTO user)
        {
            // Chọn emoji dựa vào streak
            string emoji = user.CurrentStreak >= 30 ? "🔥🔥🔥" :
                          user.CurrentStreak >= 7 ? "🔥🔥" : "🔥";

            // Chọn tone message dựa vào số freeze tokens
            string urgencyMessage = user.FreezeTokens == 0
                ? "<p style='color: #e74c3c; font-weight: bold;'>⚠️ Bạn không còn freeze token! Nếu bỏ lỡ hôm nay, chuỗi học tập sẽ bị mất.</p>"
                : user.FreezeTokens == 1
                    ? "<p style='color: #f39c12;'>⚡ Bạn còn 1 freeze token. Hãy cố gắng học hôm nay để giữ chuỗi nhé!</p>"
                    : $"<p>💎 Bạn có {user.FreezeTokens} freeze tokens để bảo vệ chuỗi học tập.</p>";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: white; padding: 30px; border-radius: 0 0 10px 10px; }}
        .streak-badge {{ font-size: 48px; font-weight: bold; color: #667eea; text-align: center; margin: 20px 0; }}
        .cta-button {{ display: inline-block; background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 20px; color: #999; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{emoji} Nhắc nhở học tập</h1>
            <p>Chào {user.FullName}!</p>
        </div>
        <div class='content'>
            <div class='streak-badge'>
                {user.CurrentStreak} ngày liên tiếp
            </div>
            
            <p>{user.ReminderMessage}</p>
            
            {urgencyMessage}
            
            <p>Chỉ cần <strong>hoàn thành 1 bài tập</strong> hôm nay để tiếp tục chuỗi học tập của bạn!</p>
            
            <div style='text-align: center;'>
                <a href='http://localhost:4200/homepage' class='cta-button'>
                    🚀 Bắt đầu học ngay
                </a>
            </div>
            
            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
            
            <p style='font-size: 14px; color: #666;'>
                <strong>💡 Lời khuyên:</strong> Hãy dành 15-20 phút mỗi ngày để duy trì thói quen học tập. 
                Sự kiên trì là chìa khóa của thành công!
            </p>
        </div>
        <div class='footer'>
            <p>Đây là email tự động từ Lumina TOEIC</p>
            <p>Nếu bạn không muốn nhận email nhắc nhở, vui lòng cập nhật cài đặt trong tài khoản.</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}