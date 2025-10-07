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

        public async Task<int> CreateAsync(SlideDTO dto)
        {
            
            return await _slideRepository.CreateAsync(dto);
        }

        public async Task<bool> UpdateAsync( SlideDTO dto)
        {
            
            return await _slideRepository.UpdateAsync(dto);
        }

        public Task<bool> DeleteAsync(int slideId)
            => _slideRepository.DeleteAsync(slideId);
    }
} 