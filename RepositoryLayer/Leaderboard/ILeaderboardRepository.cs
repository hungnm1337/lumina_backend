using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Leaderboard
{
    public interface ILeaderboardRepository
    {
        Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10);
        Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null);
        Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId);
        Task<LeaderboardDTO?> GetCurrentAsync();
        Task<int> CreateAsync(DataLayer.Models.Leaderboard entity);
        Task<bool> UpdateAsync(DataLayer.Models.Leaderboard entity);
        Task<bool> DeleteAsync(int leaderboardId);
        Task<bool> SetCurrentAsync(int leaderboardId);
        Task<bool> ExistsSeasonNumberAsync(int seasonNumber, int? excludeId = null);
        Task<bool> ExistsDateOverlapAsync(DateTime? start, DateTime? end, int? excludeId = null);
        Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100);
        Task<int> RecalculateSeasonScoresAsync(int leaderboardId);
        
        // Season-specific methods
        Task<int> ResetSeasonScoresAsync(int leaderboardId, bool archiveScores = true);
        Task<UserSeasonStatsDTO?> GetUserSeasonStatsAsync(int userId, int leaderboardId);
        Task<TOEICScoreCalculationDTO?> GetUserTOEICCalculationAsync(int userId, int leaderboardId);
        Task<int> GetUserRankInSeasonAsync(int userId, int leaderboardId);
        Task<bool> IsSeasonActiveAsync(int leaderboardId);
        Task AutoActivateSeasonAsync(); 
        Task AutoEndSeasonAsync(); 
    }
}
