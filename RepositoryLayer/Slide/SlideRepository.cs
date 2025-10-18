using DataLayer.DTOs;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Slide
{
    public class SlideRepository : ISlideRepository
    {
        private readonly LuminaSystemContext _context;

        public SlideRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<List<SlideDTO>> GetAllAsync(string? keyword = null, bool? isActive = null)
        {
            var query = _context.Slides.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(s => s.SlideName.Contains(key) || s.SlideUrl.Contains(key));
            }
            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var slides = await query
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync();

            return slides.Select(s => new SlideDTO
            {
                SlideId = s.SlideId,
                SlideUrl = s.SlideUrl,
                SlideName = s.SlideName,
                UpdateBy = s.UpdateBy,
                CreateBy = s.CreateBy,
                IsActive = s.IsActive,
                UpdateAt = s.UpdateAt,
                CreateAt = s.CreateAt
            }).ToList();
        }

        public async Task<SlideDTO?> GetByIdAsync(int slideId)
        {
            var s = await _context.Slides.FirstOrDefaultAsync(x => x.SlideId == slideId);
            if (s == null) return null;
            return new SlideDTO
            {
                SlideId = s.SlideId,
                SlideUrl = s.SlideUrl,
                SlideName = s.SlideName,
                UpdateBy = s.UpdateBy,
                CreateBy = s.CreateBy,
                IsActive = s.IsActive,
                UpdateAt = s.UpdateAt,
                CreateAt = s.CreateAt
            };
        }

        public async Task<int> CreateAsync(SlideDTO entity)
        {
            var newSlide = new DataLayer.Models.Slide()
            {
                CreateAt = DateTime.UtcNow,
                CreateBy = entity.CreateBy,
                IsActive = entity.IsActive,
                SlideName = entity.SlideName,
                UpdateBy = null,
                UpdateAt = null,
                SlideUrl = entity.SlideUrl
            };
            _context.Slides.Add(newSlide);
            await _context.SaveChangesAsync();
            return newSlide.SlideId;
        }

        public async Task<bool> UpdateAsync(SlideDTO entity)
        {
            var slide = await _context.Slides.FirstOrDefaultAsync(s => s.SlideId == entity.SlideId);
            if (slide == null) return false;
            slide.SlideName = entity.SlideName;
            slide.SlideUrl = entity.SlideUrl;
            slide.IsActive = entity.IsActive;
            slide.UpdateBy = entity.UpdateBy;
            slide.UpdateAt = DateTime.UtcNow;

            _context.Slides.Update(slide);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int slideId)
        {
            var existing = await _context.Slides.FirstOrDefaultAsync(s => s.SlideId == slideId);
            if (existing == null) return false;
            _context.Slides.Remove(existing);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
} 