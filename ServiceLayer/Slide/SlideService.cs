using DataLayer.DTOs;
using RepositoryLayer.Slide;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Slide
{
    public class SlideService : ISlideService
    {
        private readonly ISlideRepository _slideRepository;

        public SlideService(ISlideRepository slideRepository)
        {
            _slideRepository = slideRepository;
        }

        public Task<List<SlideDTO>> GetAllAsync(string? keyword = null, bool? isActive = null)
            => _slideRepository.GetAllAsync(keyword, isActive);

        public Task<SlideDTO?> GetByIdAsync(int slideId)
            => _slideRepository.GetByIdAsync(slideId);

        public async Task<int> CreateAsync(SlideDTO dto, int userId)
        {
            var entity = new DataLayer.Models.Slide
            {
                SlideUrl = dto.SlideUrl,
                SlideName = dto.SlideName,
                CreateBy = userId,
                CreateAt = DateTime.UtcNow,
                IsActive = dto.IsActive ?? true
            };
            return await _slideRepository.CreateAsync(entity);
        }

        public async Task<bool> UpdateAsync(int slideId, SlideDTO dto, int userId)
        {
            var existing = await _slideRepository.GetByIdAsync(slideId);
            if (existing == null) return false;

            var entity = new DataLayer.Models.Slide
            {
                SlideId = slideId,
                SlideUrl = dto.SlideUrl,
                SlideName = dto.SlideName,
                CreateBy = existing.CreateBy,
                CreateAt = existing.CreateAt,
                UpdateBy = userId,
                UpdateAt = DateTime.UtcNow,
                IsActive = dto.IsActive ?? existing.IsActive
            };
            return await _slideRepository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int slideId)
            => _slideRepository.DeleteAsync(slideId);
    }
} 