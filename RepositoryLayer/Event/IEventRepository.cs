using DataLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Event
{
    public interface IEventRepository
    {
        Task<List<EventDTO>> GetAllAsync(DateTime? from = null, DateTime? to = null, string? keyword = null);
        Task<EventDTO?> GetByIdAsync(int eventId);
        Task<int> CreateAsync(DataLayer.Models.Event entity);
        Task<bool> UpdateAsync(DataLayer.Models.Event entity);
        Task<bool> DeleteAsync(int eventId);
    }
}
