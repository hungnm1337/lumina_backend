using DataLayer.DTOs;
using DataLayer.Models;
using RepositoryLayer.Event;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Event
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public Task<List<EventDTO>> GetAllAsync(DateTime? from = null, DateTime? to = null, string? keyword = null)
            => _eventRepository.GetAllAsync(from, to, keyword);

        public Task<PaginatedResultDTO<EventDTO>> GetAllPaginatedAsync(DateTime? from = null, DateTime? to = null, string? keyword = null, int page = 1, int pageSize = 10)
            => _eventRepository.GetAllPaginatedAsync(from, to, keyword, page, pageSize);

        public Task<EventDTO?> GetByIdAsync(int eventId)
            => _eventRepository.GetByIdAsync(eventId);

        public async Task<int> CreateAsync(EventDTO dto, int userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.EventName == null)
                throw new ArgumentNullException(nameof(dto.EventName), "EventName cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.EventName))
                throw new ArgumentException("EventName cannot be empty or whitespace.", nameof(dto.EventName));

            var now = DateTime.UtcNow;
            var entity = new DataLayer.Models.Event
            {
                EventName = dto.EventName,
                Content = dto.Content,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreateAt = now,
                UpdateAt = null,
                CreateBy = userId,
                UpdateBy = null
            };

            var id = await _eventRepository.CreateAsync(entity);
            return id;
        }

        public async Task<bool> UpdateAsync(int eventId, EventDTO dto, int userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.EventName == null)
                throw new ArgumentNullException(nameof(dto.EventName), "EventName cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.EventName))
                throw new ArgumentException("EventName cannot be empty or whitespace.", nameof(dto.EventName));

            var existing = await _eventRepository.GetByIdAsync(eventId);
            if (existing == null) return false;

            var entity = new DataLayer.Models.Event
            {
                EventId = eventId,
                EventName = dto.EventName,
                Content = dto.Content,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreateAt = existing.CreateAt,
                UpdateAt = DateTime.UtcNow,
                CreateBy = existing.CreateBy,
                UpdateBy = userId
            };

            return await _eventRepository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int eventId)
            => _eventRepository.DeleteAsync(eventId);
    }
}
