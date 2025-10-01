using DataLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Slide
{
    public interface ISlideService
    {
        Task<List<SlideDTO>> GetAllAsync(string? keyword = null, bool? isActive = null);
        Task<SlideDTO?> GetByIdAsync(int slideId);
        Task<int> CreateAsync(SlideDTO dto, int userId);
        Task<bool> UpdateAsync(int slideId, SlideDTO dto, int userId);
        Task<bool> DeleteAsync(int slideId);
    }
} 