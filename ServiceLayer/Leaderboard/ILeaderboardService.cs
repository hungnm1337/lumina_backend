using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public interface ILeaderboardService
    {
        Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10);
        Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null);
        Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId);
        Task<LeaderboardDTO?> GetCurrentAsync();
        Task<int> CreateAsync(CreateLeaderboardDTO dto);
        Task<bool> UpdateAsync(int leaderboardId, UpdateLeaderboardDTO dto);
        Task<bool> DeleteAsync(int leaderboardId);
        Task<bool> SetCurrentAsync(int leaderboardId);
        Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100);
        Task<int> RecalculateSeasonScoresAsync(int leaderboardId);
    }
}
