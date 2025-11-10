# 🔔 Tích Hợp Thông Báo với Hệ Thống Mùa Giải

## 📋 Tổng Quan

Tài liệu này mô tả cách tích hợp hệ thống thông báo với chức năng mùa giải để gửi thông báo tự động cho người dùng khi:
- Đạt mốc điểm TOEIC mới
- Thứ hạng thay đổi quan trọng
- Mùa giải sắp kết thúc
- Nhận thưởng từ mùa giải

## 🎯 Các Loại Thông Báo

### 1. Thông Báo Tiến Độ (Progress Notifications)

#### Mốc TOEIC Mới
```csharp
public enum TOEICMilestone
{
    Beginner_100 = 100,
    Elementary_250 = 250,
    Intermediate_450 = 450,
    UpperIntermediate_650 = 650,
    Advanced_750 = 750,
    Proficient_850 = 850,
    Near_Perfect_950 = 950
}

public class NotificationMessage
{
    public int TOEICScore { get; set; }
    public string Level { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Icon { get; set; }
    public string Type { get; set; } // success, info, warning
}
```

#### Template Thông Báo Theo Mốc

```csharp
private static Dictionary<string, NotificationMessage> NotificationTemplates = new()
{
    // Beginner (0-200)
    ["Beginner_100"] = new NotificationMessage
    {
        Title = "🎉 Chào mừng bạn đến với Lumina!",
        Message = "Bạn đã bắt đầu hành trình chinh phục TOEIC! Tiếp tục nỗ lực nhé! 💪",
        Type = "success",
        Icon = "🌟"
    },
    
    // Elementary (201-400)
    ["Elementary_250"] = new NotificationMessage
    {
        Title = "📈 Bạn đang tiến bộ!",
        Message = "Ước tính TOEIC của bạn: 250 điểm. Bạn đã vượt qua mốc Beginner! 🎯",
        Type = "info",
        Icon = "🚀"
    },
    
    // Intermediate (401-600)
    ["Intermediate_450"] = new NotificationMessage
    {
        Title = "💪 Trình độ trung bình!",
        Message = "Tuyệt vời! Bạn đã đạt 450 điểm TOEIC. Hãy tiếp tục luyện tập! 📚",
        Type = "info",
        Icon = "📖"
    },
    
    // Upper-Intermediate (601-750)
    ["UpperIntermediate_650"] = new NotificationMessage
    {
        Title = "🎊 Khá tốt rồi đấy!",
        Message = "Bạn đã đạt 650 điểm! Bạn đang ở mức Upper-Intermediate. Sẵn sàng đi thi thật chưa? 🎓",
        Type = "success",
        Icon = "🏆"
    },
    
    // Advanced (751-850)
    ["Advanced_750"] = new NotificationMessage
    {
        Title = "🌟 Sẵn sàng thi TOEIC!",
        Message = "Xuất sắc! 750 điểm - Bạn đã sẵn sàng cho kỳ thi TOEIC thực tế! 🎯",
        Type = "success",
        Icon = "🎖️"
    },
    
    // Proficient (851-990)
    ["Proficient_850"] = new NotificationMessage
    {
        Title = "🏅 Trình độ xuất sắc!",
        Message = "Wow! 850+ điểm! Bạn đã đạt trình độ Proficient. Hãy chinh phục 990 điểm! 🔥",
        Type = "success",
        Icon = "👑"
    },
    
    ["Near_Perfect_950"] = new NotificationMessage
    {
        Title = "👑 Gần hoàn hảo!",
        Message = "Không thể tin được! 950 điểm! Bạn đã gần đạt điểm tuyệt đối rồi! 🌟✨",
        Type = "success",
        Icon = "💎"
    }
};
```

### 2. Thông Báo Xếp Hạng (Ranking Notifications)

```csharp
public class RankingNotificationConfig
{
    // Thông báo khi lên hạng
    public static string RankUp(int oldRank, int newRank, string seasonName)
    {
        var improvement = oldRank - newRank;
        if (improvement >= 100)
            return $"🚀 Bùng nổ! Bạn đã vượt {improvement} người trong {seasonName}!";
        else if (improvement >= 50)
            return $"📈 Tuyệt vời! Bạn đã tăng {improvement} bậc xếp hạng!";
        else if (improvement >= 10)
            return $"⬆️ Tiến bộ! Bạn đã lên từ #{oldRank} → #{newRank}";
        else
            return $"👍 Bạn đã lên hạng #{newRank}!";
    }
    
    // Thông báo khi xuống hạng
    public static string RankDown(int oldRank, int newRank)
    {
        var drop = newRank - oldRank;
        if (drop >= 100)
            return $"⚠️ Cẩn thận! Bạn đã tụt {drop} bậc. Hãy cố gắng hơn nhé!";
        else if (drop >= 50)
            return $"📉 Bạn đã xuống {drop} bậc. Đừng từ bỏ!";
        else
            return $"↓ Bạn đã xuống hạng #{newRank}. Hãy luyện tập thêm!";
    }
    
    // Thông báo khi vào Top
    public static string EnterTop(int rank, string tier)
    {
        return rank switch
        {
            1 => "👑 CHÚC MỪNG! Bạn đã lên #1 BXH! Thật xuất sắc!",
            <= 3 => $"🥇 WOW! Bạn đã vào TOP 3! (#{rank})",
            <= 10 => $"🏆 Tuyệt vời! Bạn đã vào TOP 10! (#{rank})",
            <= 50 => $"🌟 Ấn tượng! Bạn đã vào TOP 50! (#{rank})",
            <= 100 => $"⭐ Giỏi lắm! Bạn đã vào TOP 100! (#{rank})",
            _ => $"📊 Bạn hiện đang ở hạng #{rank}"
        };
    }
}
```

### 3. Thông Báo Mùa Giải (Season Notifications)

```csharp
public class SeasonNotificationConfig
{
    // Khi mùa mới bắt đầu
    public static NotificationMessage SeasonStart(string seasonName, DateTime endDate)
    {
        return new NotificationMessage
        {
            Title = $"🎊 {seasonName} đã bắt đầu!",
            Message = $"Mùa giải mới đã được mở! Hãy tham gia và leo lên BXH! Kết thúc: {endDate:dd/MM/yyyy}",
            Type = "info",
            Icon = "🎉"
        };
    }
    
    // Khi mùa sắp kết thúc (3 ngày)
    public static NotificationMessage SeasonEnding3Days(string seasonName, int currentRank)
    {
        return new NotificationMessage
        {
            Title = "⏰ Mùa giải sắp kết thúc!",
            Message = $"{seasonName} sẽ kết thúc trong 3 ngày! Hạng hiện tại: #{currentRank}. Cố gắng lên!",
            Type = "warning",
            Icon = "⏳"
        };
    }
    
    // Khi mùa sắp kết thúc (1 ngày)
    public static NotificationMessage SeasonEnding1Day(string seasonName, int currentRank)
    {
        return new NotificationMessage
        {
            Title = "🚨 Chỉ còn 1 ngày!",
            Message = $"NHANH LÊN! {seasonName} kết thúc sau 24 giờ! Hạng #{currentRank}",
            Type = "warning",
            Icon = "⏰"
        };
    }
    
    // Khi mùa kết thúc
    public static NotificationMessage SeasonEnded(string seasonName, int finalRank, int totalParticipants)
    {
        var percentile = (1 - (double)finalRank / totalParticipants) * 100;
        return new NotificationMessage
        {
            Title = $"🏁 {seasonName} đã kết thúc!",
            Message = $"Kết quả cuối: Hạng #{finalRank}/{totalParticipants} (Top {percentile:F1}%). Cảm ơn bạn đã tham gia!",
            Type = "info",
            Icon = "🎊"
        };
    }
    
    // Khi nhận thưởng
    public static NotificationMessage SeasonReward(string seasonName, int finalRank, string rewardType)
    {
        return new NotificationMessage
        {
            Title = "🎁 Bạn có phần thưởng!",
            Message = $"Chúc mừng! Bạn đã nhận {rewardType} từ {seasonName} (Hạng #{finalRank})!",
            Type = "success",
            Icon = "🏆"
        };
    }
}
```

## 🔧 Implementation

### 1. Service Layer - Notification Integration

Tạo file `LeaderboardNotificationService.cs`:

```csharp
using DataLayer.Models;
using DataLayer.DTOs.Leaderboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public interface ILeaderboardNotificationService
    {
        Task CheckAndSendTOEICMilestoneNotificationAsync(int userId, int leaderboardId);
        Task CheckAndSendRankingNotificationAsync(int userId, int leaderboardId, int oldRank, int newRank);
        Task SendSeasonStartNotificationAsync(int leaderboardId);
        Task SendSeasonEndingNotificationsAsync(int leaderboardId);
        Task SendSeasonEndedNotificationsAsync(int leaderboardId);
    }
    
    public class LeaderboardNotificationService : ILeaderboardNotificationService
    {
        private readonly LuminaSystemContext _context;
        private readonly ILeaderboardRepository _leaderboardRepo;
        
        public LeaderboardNotificationService(
            LuminaSystemContext context,
            ILeaderboardRepository leaderboardRepo)
        {
            _context = context;
            _leaderboardRepo = leaderboardRepo;
        }
        
        public async Task CheckAndSendTOEICMilestoneNotificationAsync(int userId, int leaderboardId)
        {
            // Lấy điểm TOEIC hiện tại
            var calculation = await _leaderboardRepo.GetUserTOEICCalculationAsync(userId, leaderboardId);
            if (calculation == null) return;
            
            var currentScore = calculation.EstimatedTOEICScore;
            
            // Kiểm tra xem user đã nhận thông báo mốc này chưa
            var lastNotified = await GetLastNotifiedTOEICScore(userId, leaderboardId);
            
            // Xác định mốc đã đạt
            var milestone = GetTOEICMilestone(currentScore);
            var lastMilestone = GetTOEICMilestone(lastNotified);
            
            // Nếu đạt mốc mới
            if (milestone > lastMilestone)
            {
                var template = GetNotificationTemplate(milestone);
                if (template != null)
                {
                    await CreateNotification(userId, template);
                    await UpdateLastNotifiedTOEICScore(userId, leaderboardId, currentScore);
                }
            }
        }
        
        public async Task CheckAndSendRankingNotificationAsync(
            int userId, 
            int leaderboardId, 
            int oldRank, 
            int newRank)
        {
            if (oldRank == newRank) return;
            
            var season = await _context.Leaderboards.FindAsync(leaderboardId);
            if (season == null) return;
            
            string message;
            string type;
            
            if (newRank < oldRank) // Lên hạng
            {
                message = RankingNotificationConfig.RankUp(oldRank, newRank, season.SeasonName ?? "mùa giải");
                type = "success";
                
                // Check if entered top tier
                if (newRank <= 100 && oldRank > 100)
                {
                    message = RankingNotificationConfig.EnterTop(newRank, "Top 100");
                    type = "success";
                }
            }
            else // Xuống hạng
            {
                message = RankingNotificationConfig.RankDown(oldRank, newRank);
                type = "warning";
            }
            
            await CreateNotification(userId, new NotificationMessage
            {
                Title = "📊 Thứ hạng đã thay đổi!",
                Message = message,
                Type = type,
                Icon = newRank < oldRank ? "📈" : "📉"
            });
        }
        
        public async Task SendSeasonStartNotificationAsync(int leaderboardId)
        {
            var season = await _context.Leaderboards.FindAsync(leaderboardId);
            if (season == null) return;
            
            // Gửi cho tất cả users đã từng tham gia hoặc có gói Pro
            var users = await _context.Users
                .Where(u => u.Role.RoleName == "User" || u.Role.RoleName == "Pro")
                .Select(u => u.UserId)
                .ToListAsync();
            
            var template = SeasonNotificationConfig.SeasonStart(
                season.SeasonName ?? $"Season {season.SeasonNumber}",
                season.EndDate ?? DateTime.UtcNow.AddMonths(1)
            );
            
            foreach (var userId in users)
            {
                await CreateNotification(userId, template);
            }
        }
        
        public async Task SendSeasonEndingNotificationsAsync(int leaderboardId)
        {
            var season = await _context.Leaderboards.FindAsync(leaderboardId);
            if (season == null || !season.EndDate.HasValue) return;
            
            var daysRemaining = (season.EndDate.Value - DateTime.UtcNow).Days;
            
            if (daysRemaining != 3 && daysRemaining != 1) return;
            
            // Lấy tất cả participants
            var participants = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId)
                .ToListAsync();
            
            foreach (var participant in participants)
            {
                var rank = await _leaderboardRepo.GetUserRankInSeasonAsync(participant.UserId, leaderboardId);
                
                var template = daysRemaining == 3
                    ? SeasonNotificationConfig.SeasonEnding3Days(season.SeasonName ?? "Mùa giải", rank)
                    : SeasonNotificationConfig.SeasonEnding1Day(season.SeasonName ?? "Mùa giải", rank);
                
                await CreateNotification(participant.UserId, template);
            }
        }
        
        public async Task SendSeasonEndedNotificationsAsync(int leaderboardId)
        {
            var season = await _context.Leaderboards.FindAsync(leaderboardId);
            if (season == null) return;
            
            var participants = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId)
                .ToListAsync();
            
            var totalParticipants = participants.Count;
            
            foreach (var participant in participants)
            {
                var rank = await _leaderboardRepo.GetUserRankInSeasonAsync(participant.UserId, leaderboardId);
                
                var template = SeasonNotificationConfig.SeasonEnded(
                    season.SeasonName ?? "Mùa giải",
                    rank,
                    totalParticipants
                );
                
                await CreateNotification(participant.UserId, template);
                
                // Nếu trong top, gửi thông báo nhận thưởng
                if (rank <= 100)
                {
                    var rewardType = rank switch
                    {
                        1 => "Huy hiệu Vàng + 1000 Kim Cương",
                        <= 3 => "Huy hiệu Bạc + 500 Kim Cương",
                        <= 10 => "Huy hiệu Đồng + 200 Kim Cương",
                        <= 50 => "100 Kim Cương",
                        _ => "Danh hiệu Top 100"
                    };
                    
                    var rewardTemplate = SeasonNotificationConfig.SeasonReward(
                        season.SeasonName ?? "Mùa giải",
                        rank,
                        rewardType
                    );
                    
                    await CreateNotification(participant.UserId, rewardTemplate);
                }
            }
        }
        
        // Helper methods
        private async Task CreateNotification(int userId, NotificationMessage template)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = template.Title,
                Message = template.Message,
                Type = template.Type,
                IsRead = false,
                CreateAt = DateTime.UtcNow
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        
        private async Task<int> GetLastNotifiedTOEICScore(int userId, int leaderboardId)
        {
            // TODO: Lưu trong bảng UserSeasonProgress hoặc UserMetadata
            // Tạm thời return 0
            return 0;
        }
        
        private async Task UpdateLastNotifiedTOEICScore(int userId, int leaderboardId, int score)
        {
            // TODO: Update vào bảng UserSeasonProgress
        }
        
        private int GetTOEICMilestone(int score)
        {
            return score switch
            {
                >= 950 => 950,
                >= 850 => 850,
                >= 750 => 750,
                >= 650 => 650,
                >= 450 => 450,
                >= 250 => 250,
                >= 100 => 100,
                _ => 0
            };
        }
        
        private NotificationMessage? GetNotificationTemplate(int milestone)
        {
            var key = milestone switch
            {
                950 => "Near_Perfect_950",
                850 => "Proficient_850",
                750 => "Advanced_750",
                650 => "UpperIntermediate_650",
                450 => "Intermediate_450",
                250 => "Elementary_250",
                100 => "Beginner_100",
                _ => null
            };
            
            return key != null && NotificationTemplates.ContainsKey(key) 
                ? NotificationTemplates[key] 
                : null;
        }
    }
}
```

### 2. Trigger Notifications

Cập nhật `RecalculateSeasonScoresAsync` để trigger notifications:

```csharp
public async Task<int> RecalculateSeasonScoresAsync(int leaderboardId)
{
    // ... existing code ...
    
    // Sau khi tính điểm xong, check notifications
    foreach (var kvp in userScores)
    {
        var userId = kvp.Key;
        var oldRank = await GetUserRankInSeasonAsync(userId, leaderboardId);
        
        // Update score
        // ...
        
        var newRank = await GetUserRankInSeasonAsync(userId, leaderboardId);
        
        // Send notifications
        await _notificationService.CheckAndSendTOEICMilestoneNotificationAsync(userId, leaderboardId);
        await _notificationService.CheckAndSendRankingNotificationAsync(userId, leaderboardId, oldRank, newRank);
    }
    
    return await _context.SaveChangesAsync();
}
```

### 3. Background Job Configuration

Thêm vào `Program.cs`:

```csharp
// Hangfire jobs
RecurringJob.AddOrUpdate<ILeaderboardService>(
    "auto-manage-seasons",
    service => service.AutoManageSeasonsAsync(),
    Cron.Hourly
);

RecurringJob.AddOrUpdate<ILeaderboardNotificationService>(
    "send-season-ending-notifications",
    service => service.SendSeasonEndingNotificationsAsync(/* current season id */),
    "0 9,18 * * *" // 9 AM và 6 PM mỗi ngày
);
```

## 📱 Frontend Integration

### Notification Component Example (Angular)

```typescript
export interface SeasonNotification {
  id: number;
  title: string;
  message: string;
  type: 'success' | 'info' | 'warning';
  icon: string;
  isRead: boolean;
  createAt: Date;
}

@Component({
  selector: 'app-season-notification',
  template: `
    <div class="notification" [class]="'notification-' + notification.type">
      <span class="icon">{{ notification.icon }}</span>
      <div class="content">
        <h4>{{ notification.title }}</h4>
        <p>{{ notification.message }}</p>
      </div>
      <button (click)="markAsRead()">✓</button>
    </div>
  `
})
export class SeasonNotificationComponent {
  @Input() notification!: SeasonNotification;
  
  markAsRead() {
    // Call API to mark as read
  }
}
```

## 🧪 Testing

### Unit Tests

```csharp
[Fact]
public async Task SendTOEICMilestoneNotification_WhenReachingNewMilestone_ShouldCreateNotification()
{
    // Arrange
    var userId = 123;
    var leaderboardId = 1;
    var service = new LeaderboardNotificationService(_context, _leaderboardRepo);
    
    // Act
    await service.CheckAndSendTOEICMilestoneNotificationAsync(userId, leaderboardId);
    
    // Assert
    var notification = await _context.Notifications
        .FirstOrDefaultAsync(n => n.UserId == userId);
    
    Assert.NotNull(notification);
    Assert.Contains("TOEIC", notification.Title);
}
```

## 📊 Metrics to Track

- **Notification Open Rate**: Tỷ lệ mở thông báo
- **Engagement After Notification**: Số lượng bài làm sau khi nhận thông báo
- **Notification Opt-out Rate**: Tỷ lệ tắt thông báo
- **Peak Notification Times**: Thời điểm nhận nhiều thông báo nhất

---

**Version:** 1.0  
**Last Updated:** October 30, 2025  
**Author:** Lumina Development Team
