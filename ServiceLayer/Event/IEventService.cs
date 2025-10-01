using DataLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Event
{
    public interface IEventService
    {
        Task<List<EventDTO>> GetAllAsync(DateTime? from = null, DateTime? to = null, string? keyword = null);
        Task<EventDTO?> GetByIdAsync(int eventId);
        Task<int> CreateAsync(EventDTO dto, int userId);
        Task<bool> UpdateAsync(int eventId, EventDTO dto, int userId);
        Task<bool> DeleteAsync(int eventId);
    }
}
