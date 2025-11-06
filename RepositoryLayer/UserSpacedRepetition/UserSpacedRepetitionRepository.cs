using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.UserSpacedRepetition
{
    public class UserSpacedRepetitionRepository : IUserSpacedRepetitionRepository
    {
        private readonly LuminaSystemContext _context;

        public UserSpacedRepetitionRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<DataLayer.Models.UserSpacedRepetition?> GetByUserAndListAsync(int userId, int vocabularyListId)
        {
            return await _context.UserSpacedRepetitions
                .Include(usr => usr.VocabularyList)
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.VocabularyListId == vocabularyListId);
        }

        public async Task<IEnumerable<DataLayer.Models.UserSpacedRepetition>> GetDueForReviewAsync(int userId)
        {
            var now = DateTime.UtcNow;
            return await _context.UserSpacedRepetitions
                .Where(usr => usr.UserId == userId 
                    && usr.NextReviewAt.HasValue 
                    && usr.NextReviewAt.Value <= now
                    && (usr.Status == "New" || usr.Status == "Learning")
                    && usr.ReviewCount > 0  // Chỉ lấy những từ đã được review
                    && (usr.Intervals == 1 || usr.Intervals == null))  // Chỉ lấy những từ đánh giá thấp (intervals = 1)
                .Include(usr => usr.VocabularyList)
                .OrderBy(usr => usr.NextReviewAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DataLayer.Models.UserSpacedRepetition>> GetByUserIdAsync(int userId)
        {
            return await _context.UserSpacedRepetitions
                .Where(usr => usr.UserId == userId)
                .Include(usr => usr.VocabularyList)
                .OrderByDescending(usr => usr.NextReviewAt)
                .ToListAsync();
        }

        public async Task<DataLayer.Models.UserSpacedRepetition?> GetByIdAsync(int id)
        {
            return await _context.UserSpacedRepetitions
                .Include(usr => usr.VocabularyList)
                .FirstOrDefaultAsync(usr => usr.UserSpacedRepetitionId == id);
        }

        public async Task<DataLayer.Models.UserSpacedRepetition> AddAsync(DataLayer.Models.UserSpacedRepetition entity)
        {
            await _context.UserSpacedRepetitions.AddAsync(entity);
            return entity;
        }

        public async Task<DataLayer.Models.UserSpacedRepetition> UpdateAsync(DataLayer.Models.UserSpacedRepetition entity)
        {
            _context.UserSpacedRepetitions.Update(entity);
            return entity;
        }

        public async Task<bool> ExistsAsync(int userId, int vocabularyListId)
        {
            return await _context.UserSpacedRepetitions
                .AnyAsync(usr => usr.UserId == userId && usr.VocabularyListId == vocabularyListId);
        }
    }
}

