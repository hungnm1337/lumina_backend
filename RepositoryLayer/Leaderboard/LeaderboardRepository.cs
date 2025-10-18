using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Leaderboard
{
    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly LuminaSystemContext _context;

        public LeaderboardRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Leaderboards.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(x => (x.SeasonName != null && x.SeasonName.Contains(key)) || x.SeasonNumber.ToString().Contains(key));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(MapToDto).ToList();

            return new PaginatedResultDTO<LeaderboardDTO>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }

        public async Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null)
        {
            var query = _context.Leaderboards.AsQueryable();
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }
            var items = await query.OrderByDescending(x => x.StartDate).ToListAsync();
            return items.Select(MapToDto).ToList();
        }

        public async Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId)
        {
            var entity = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<LeaderboardDTO?> GetCurrentAsync()
        {
            var now = DateTime.UtcNow;
            var entity = await _context.Leaderboards
                .Where(x => x.IsActive && (!x.StartDate.HasValue || x.StartDate <= now) && (!x.EndDate.HasValue || x.EndDate >= now))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync();
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<int> CreateAsync(DataLayer.Models.Leaderboard entity)
        {
            _context.Leaderboards.Add(entity);
            await _context.SaveChangesAsync();
            return entity.LeaderboardId;
        }

        public async Task<bool> UpdateAsync(DataLayer.Models.Leaderboard entity)
        {
            var existing = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == entity.LeaderboardId);
            if (existing == null) return false;

            existing.SeasonName = entity.SeasonName;
            existing.SeasonNumber = entity.SeasonNumber;
            existing.StartDate = entity.StartDate;
            existing.EndDate = entity.EndDate;
            existing.IsActive = entity.IsActive;
            existing.UpdateAt = entity.UpdateAt;

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int leaderboardId)
        {
            var existing = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (existing == null) return false;
            _context.Leaderboards.Remove(existing);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> SetCurrentAsync(int leaderboardId)
        {
            var target = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (target == null) return false;

            var all = await _context.Leaderboards.ToListAsync();
            foreach (var l in all)
            {
                l.IsActive = l.LeaderboardId == leaderboardId;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsSeasonNumberAsync(int seasonNumber, int? excludeId = null)
        {
            return _context.Leaderboards.AnyAsync(x => x.SeasonNumber == seasonNumber && (!excludeId.HasValue || x.LeaderboardId != excludeId.Value));
        }

        public Task<bool> ExistsDateOverlapAsync(DateTime? start, DateTime? end, int? excludeId = null)
        {
            // Two ranges [s1,e1] and [s2,e2] overlap if s1 <= e2 && s2 <= e1
            return _context.Leaderboards.AnyAsync(x =>
                (!excludeId.HasValue || x.LeaderboardId != excludeId.Value)
                && (
                    // if either has nulls, treat as open range
                    (
                        (!x.StartDate.HasValue || !end.HasValue || x.StartDate <= end)
                        && (!start.HasValue || !x.EndDate.HasValue || start <= x.EndDate)
                    )
                )
            );
        }

        private static LeaderboardDTO MapToDto(DataLayer.Models.Leaderboard e)
        {
            return new LeaderboardDTO
            {
                LeaderboardId = e.LeaderboardId,
                SeasonName = e.SeasonName,
                SeasonNumber = e.SeasonNumber,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsActive = e.IsActive,
                CreateAt = e.CreateAt,
                UpdateAt = e.UpdateAt
            };
        }

        public async Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return new List<LeaderboardRankDTO>();

            // Prefer persisted UserLeaderboards if available; otherwise aggregate from UserAnswers
            var persisted = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId)
                .OrderByDescending(ul => ul.Score)
                .Take(top)
                .Select(ul => new { ul.UserId, ul.Score })
                .ToListAsync();

            List<(int UserId, int Score)> list;
            if (persisted.Count > 0)
            {
                list = persisted.Select(x => (x.UserId, x.Score)).ToList();
            }
            else
            {
                var aggregated = await _context.UserAnswers
                    .Where(ua => (!season.StartDate.HasValue || ua.Attempt.StartTime >= season.StartDate)
                                 && (!season.EndDate.HasValue || ua.Attempt.EndTime <= season.EndDate)
                                 && ua.Score != null)
                    .GroupBy(ua => ua.Attempt.UserId)
                    .Select(g => new { UserId = g.Key, Score = (int)Math.Round(g.Sum(x => x.Score ?? 0f)) })
                    .OrderByDescending(x => x.Score)
                    .Take(top)
                    .ToListAsync();
                list = aggregated.Select(x => (x.UserId, x.Score)).ToList();
            }

            var userIds = list.Select(x => x.UserId).ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            int rank = 1;
            return list.Select(x => new LeaderboardRankDTO
            {
                UserId = x.UserId,
                FullName = users.TryGetValue(x.UserId, out var name) ? name : $"User {x.UserId}",
                Score = x.Score,
                Rank = rank++
            }).ToList();
        }

        public async Task<int> RecalculateSeasonScoresAsync(int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return 0;

            // Delete existing UserLeaderboard entries for this season
            var existing = _context.UserLeaderboards.Where(ul => ul.LeaderboardId == leaderboardId);
            _context.UserLeaderboards.RemoveRange(existing);

            // Aggregate again and insert
            var aggregated = await _context.UserAnswers
                .Where(ua => (!season.StartDate.HasValue || ua.Attempt.StartTime >= season.StartDate)
                             && (!season.EndDate.HasValue || ua.Attempt.EndTime <= season.EndDate)
                             && ua.Score != null)
                .GroupBy(ua => ua.Attempt.UserId)
                .Select(g => new { UserId = g.Key, Score = (int)Math.Round(g.Sum(x => x.Score ?? 0f)) })
                .ToListAsync();

            foreach (var a in aggregated)
            {
                _context.UserLeaderboards.Add(new DataLayer.Models.UserLeaderboard
                {
                    UserId = a.UserId,
                    LeaderboardId = leaderboardId,
                    Score = a.Score
                });
            }

            var affected = await _context.SaveChangesAsync();
            return affected;
        }
    }
}
