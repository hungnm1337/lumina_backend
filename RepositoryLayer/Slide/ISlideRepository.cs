using DataLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Slide
{
    public interface ISlideRepository
    {
        Task<List<SlideDTO>> GetAllAsync(string? keyword = null, bool? isActive = null);
        Task<SlideDTO?> GetByIdAsync(int slideId);
        Task<int> CreateAsync(DataLayer.Models.Slide entity);
        Task<bool> UpdateAsync(DataLayer.Models.Slide entity);
        Task<bool> DeleteAsync(int slideId);
    }
} 