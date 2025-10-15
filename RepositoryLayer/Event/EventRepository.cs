using DataLayer.DTOs;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Event
{
    public class EventRepository : IEventRepository
    {
        private readonly LuminaSystemContext _context;

        public EventRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<List<EventDTO>> GetAllAsync(DateTime? from = null, DateTime? to = null, string? keyword = null)
        {
            var query = _context.Events.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(e => e.StartDate >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(e => e.EndDate <= to.Value);
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(e => e.EventName.Contains(key) || (e.Content != null && e.Content.Contains(key)));
            }

            var events = await query
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            var userIds = events
                .SelectMany(e => new[] { e.CreateBy }.Concat(e.UpdateBy.HasValue ? new[] { e.UpdateBy.Value } : Array.Empty<int>()))
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            return events.Select(e => new EventDTO
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Content = e.Content,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CreateAt = e.CreateAt,
                UpdateAt = e.UpdateAt,
                CreateBy = e.CreateBy,
                UpdateBy = e.UpdateBy
            }).ToList();
        }

        public async Task<PaginatedResultDTO<EventDTO>> GetAllPaginatedAsync(DateTime? from = null, DateTime? to = null, string? keyword = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Events.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(e => e.StartDate >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(e => e.EndDate <= to.Value);
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(e => e.EventName.Contains(key) || (e.Content != null && e.Content.Contains(key)));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var events = await query
                .OrderByDescending(e => e.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = events
                .SelectMany(e => new[] { e.CreateBy }.Concat(e.UpdateBy.HasValue ? new[] { e.UpdateBy.Value } : Array.Empty<int>()))
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var eventDTOs = events.Select(e => new EventDTO
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Content = e.Content,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CreateAt = e.CreateAt,
                UpdateAt = e.UpdateAt,
                CreateBy = e.CreateBy,
                UpdateBy = e.UpdateBy
            }).ToList();

            return new PaginatedResultDTO<EventDTO>
            {
                Items = eventDTOs,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }

        public async Task<EventDTO?> GetByIdAsync(int eventId)
        {
            var e = await _context.Events.FirstOrDefaultAsync(x => x.EventId == eventId);
            if (e == null) return null;

            return new EventDTO
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Content = e.Content,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CreateAt = e.CreateAt,
                UpdateAt = e.UpdateAt,
                CreateBy = e.CreateBy,
                UpdateBy = e.UpdateBy
            };
        }

        public async Task<int> CreateAsync(DataLayer.Models.Event entity)
        {
            _context.Events.Add(entity);
            await _context.SaveChangesAsync();
            return entity.EventId;
        }

        public async Task<bool> UpdateAsync(DataLayer.Models.Event entity)
        {
            // Load existing tracked entity to avoid EF Core tracking conflicts
            var existing = await _context.Events.FirstOrDefaultAsync(e => e.EventId == entity.EventId);
            if (existing == null) return false;

            // Map fields
            existing.EventName = entity.EventName;
            existing.Content = entity.Content;
            existing.StartDate = entity.StartDate;
            existing.EndDate = entity.EndDate;
            existing.UpdateAt = entity.UpdateAt;
            existing.UpdateBy = entity.UpdateBy;
            // Preserve existing.CreateAt and existing.CreateBy

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int eventId)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (existing == null) return false;

            _context.Events.Remove(existing);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}
