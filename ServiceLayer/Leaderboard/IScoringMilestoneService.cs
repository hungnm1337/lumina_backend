using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public interface IScoringMilestoneService
    {
        Task<List<ScoringMilestoneDTO>> GetAllAsync();
        Task<ScoringMilestoneDTO?> GetByIdAsync(int milestoneId);
        Task<int> CreateAsync(CreateScoringMilestoneDTO dto);
        Task<bool> UpdateAsync(int milestoneId, UpdateScoringMilestoneDTO dto);
        Task<bool> DeleteAsync(int milestoneId);
        Task<List<UserMilestoneNotificationDTO>> GetUserNotificationsAsync(int userId);
        Task<bool> MarkNotificationAsReadAsync(int notificationId);
        Task CheckAndCreateMilestoneNotificationsAsync(int userId, int currentScore);
        Task InitializeDefaultMilestonesAsync();
    }
}

