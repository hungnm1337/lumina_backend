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

        public Task<EventDTO?> GetByIdAsync(int eventId)
            => _eventRepository.GetByIdAsync(eventId);

        public async Task<int> CreateAsync(EventDTO dto, int userId)
        {
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
