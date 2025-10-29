using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using RepositoryLayer.Leaderboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ILeaderboardRepository _repository;

        public LeaderboardService(ILeaderboardRepository repository)
        {
            _repository = repository;
        }

        public Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10)
            => _repository.GetAllPaginatedAsync(keyword, page, pageSize);

        public Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null)
            => _repository.GetAllAsync(isActive);

        public Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId)
            => _repository.GetByIdAsync(leaderboardId);

        public Task<LeaderboardDTO?> GetCurrentAsync()
            => _repository.GetCurrentAsync();

        public async Task<int> CreateAsync(CreateLeaderboardDTO dto)
        {
            await ValidateAsync(dto.SeasonNumber, dto.StartDate, dto.EndDate, null);
            var now = DateTime.UtcNow;
            var entity = new DataLayer.Models.Leaderboard
            {
                SeasonName = dto.SeasonName,
                SeasonNumber = dto.SeasonNumber,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreateAt = now,
                UpdateAt = null
            };
            return await _repository.CreateAsync(entity);
        }

        public async Task<bool> UpdateAsync(int leaderboardId, UpdateLeaderboardDTO dto)
        {
            await ValidateAsync(dto.SeasonNumber, dto.StartDate, dto.EndDate, leaderboardId);
            var entity = new DataLayer.Models.Leaderboard
            {
                LeaderboardId = leaderboardId,
                SeasonName = dto.SeasonName,
                SeasonNumber = dto.SeasonNumber,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                UpdateAt = DateTime.UtcNow
            };
            return await _repository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int leaderboardId)
            => _repository.DeleteAsync(leaderboardId);

        public Task<bool> SetCurrentAsync(int leaderboardId)
            => _repository.SetCurrentAsync(leaderboardId);

        private async Task ValidateAsync(int seasonNumber, DateTime? start, DateTime? end, int? excludeId)
        {
            if (seasonNumber <= 0) throw new ArgumentException("SeasonNumber must be positive");
            if (start.HasValue && end.HasValue && end < start) throw new ArgumentException("EndDate must be after StartDate");
            var exists = await _repository.ExistsSeasonNumberAsync(seasonNumber, excludeId);
            if (exists) throw new ArgumentException("SeasonNumber already exists");

            // Validate date overlap across all seasons (active or ended). Any intersection is not allowed.
            var overlap = await _repository.ExistsDateOverlapAsync(start, end, excludeId);
            if (overlap) throw new ArgumentException("Date range overlaps with an existing season");
        }

        public Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100)
            => _repository.GetSeasonRankingAsync(leaderboardId, top);

        public Task<int> RecalculateSeasonScoresAsync(int leaderboardId)
            => _repository.RecalculateSeasonScoresAsync(leaderboardId);

        public Task<int> ResetSeasonAsync(int leaderboardId, bool archiveScores = true)
            => _repository.ResetSeasonScoresAsync(leaderboardId, archiveScores);

        public async Task<UserSeasonStatsDTO?> GetUserStatsAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return null;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserSeasonStatsAsync(userId, leaderboardId.Value);
        }

        public async Task<TOEICScoreCalculationDTO?> GetUserTOEICCalculationAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return null;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserTOEICCalculationAsync(userId, leaderboardId.Value);
        }

        public async Task<int> GetUserRankAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return 0;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserRankInSeasonAsync(userId, leaderboardId.Value);
        }

        public async Task AutoManageSeasonsAsync()
        {
            // Tự động kích hoạt seasons đã đến ngày bắt đầu
            await _repository.AutoActivateSeasonAsync();
            
            // Tự động kết thúc seasons đã hết hạn
            await _repository.AutoEndSeasonAsync();
        }
    }
}
