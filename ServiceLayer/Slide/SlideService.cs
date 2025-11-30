using DataLayer.DTOs;
using RepositoryLayer.Slide;
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
        {
            if (slideId < 0)
            {
                throw new ArgumentException("Slide ID cannot be negative.", nameof(slideId));
            }
            return _slideRepository.GetByIdAsync(slideId);
        }

        public async Task<int> CreateAsync(SlideDTO dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }
            if (string.IsNullOrWhiteSpace(dto.SlideName))
            {
                throw new ArgumentException("Slide name cannot be empty.", nameof(dto.SlideName));
            }
            if (string.IsNullOrWhiteSpace(dto.SlideUrl))
            {
                throw new ArgumentException("Slide URL cannot be empty.", nameof(dto.SlideUrl));
            }
            return await _slideRepository.CreateAsync(dto);
        }

        public async Task<bool> UpdateAsync(SlideDTO dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }
            if (string.IsNullOrWhiteSpace(dto.SlideName))
            {
                throw new ArgumentException("Slide name cannot be empty.", nameof(dto.SlideName));
            }
            if (string.IsNullOrWhiteSpace(dto.SlideUrl))
            {
                throw new ArgumentException("Slide URL cannot be empty.", nameof(dto.SlideUrl));
            }
            return await _slideRepository.UpdateAsync(dto);
        }

        public Task<bool> DeleteAsync(int slideId)
        {
            if (slideId < 0)
            {
                throw new ArgumentException("Slide ID cannot be negative.", nameof(slideId));
            }
            return _slideRepository.DeleteAsync(slideId);
        }
    }
}