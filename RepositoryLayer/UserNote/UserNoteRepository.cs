using DataLayer.DTOs.UserNote;
using DataLayer.Models;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace RepositoryLayer.UserNote
{
    public class UserNoteRepository : IUserNoteRepository
    {
        private readonly LuminaSystemContext _context;
        public UserNoteRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<bool> AddNewUserNote(UserNoteRequestDTO userNoteRequestDTO)
        {
            DataLayer.Models.UserNote userNote = new DataLayer.Models.UserNote()
            {
                SectionId = userNoteRequestDTO.SectionId,
                UserId = userNoteRequestDTO.UserId,
                NoteContent = userNoteRequestDTO.NoteContent,
                ArticleId = userNoteRequestDTO.ArticleId,
                CreateAt = DateTime.UtcNow
            };

            _context.UserNotes.Add(userNote);

            int result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> CheckUserNoteExist(int userId, int articleId, int sectionId)
        {
            return await _context.UserNotes.AnyAsync(
            un => un.UserId == userId
            && un.ArticleId == articleId
            && un.SectionId == sectionId);
        }


        public async Task<UserNoteResponseDTO> GetUserNoteByID(int userNoteId)
        {
            try
            {
                var userNote = await _context.UserNotes
                    .Include(x => x.User)
                    .Include(x => x.Article)
                    .Include(x => x.Section)
                    .Where(un => un.NoteId == userNoteId)
                    .Select(un => new UserNoteResponseDTO()
                    {
                        NoteId = un.NoteId,
                        User = un.User.FullName,
                        UserId = un.UserId,
                        ArticleId = un.ArticleId,
                        SectionId = un.SectionId,
                        Article = un.Article.Title,
                        Section = un.Section.SectionTitle,
                        NoteContent = un.NoteContent,
                        CreateAt = un.CreateAt,
                        UpdateAt = un.UpdateAt
                    })
                    .FirstOrDefaultAsync();

                if (userNote == null)
                {
                    throw new KeyNotFoundException($"UserNote with ID {userNoteId} not found");
                }

                return userNote;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user note: {ex.Message}", ex);
            }
        }

        public async Task<UserNoteResponseDTO> GetUserNoteByUserIDAndArticleId(int userId, int articleId)
        {
            try
            {
                var userNote = await _context.UserNotes
                    .Include(x => x.User)
                    .Include(x => x.Article)
                    .Include(x => x.Section)
                    .Where(un => un.UserId == userId && un.ArticleId == articleId)
                    .Select(un => new UserNoteResponseDTO()
                    {
                        NoteId = un.NoteId,
                        User = un.User.FullName,
                        UserId = un.UserId,
                        ArticleId = un.ArticleId,
                        SectionId = un.SectionId,
                        Article = un.Article.Title,
                        Section = un.Section.SectionTitle,
                        NoteContent = un.NoteContent,
                        CreateAt = un.CreateAt,
                        UpdateAt = un.UpdateAt
                    })
                    .FirstOrDefaultAsync();

                if (userNote == null)
                {
                    throw new KeyNotFoundException($"UserNote with ID {userId} not found");
                }

                return userNote;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user note: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<UserNoteResponseDTO>> GetAllUserNotesByUserId(int userId)
        {
            try
            {
                var notes = await _context.UserNotes
                    .Include(x => x.User)
                    .Include(x => x.Article)
                    .Include(x => x.Section)
                    .AsNoTracking()
                    .Where(un => un.UserId == userId)
                    .Select(un => new UserNoteResponseDTO()
                    {
                        NoteId = un.NoteId,
                        UserId = un.UserId,
                        ArticleId = un.ArticleId,
                        SectionId = un.SectionId,
                        User = un.User.FullName,
                        Article = un.Article.Title,
                        Section = un.Section.SectionTitle,
                        NoteContent = un.NoteContent,
                        CreateAt = un.CreateAt,
                        UpdateAt = un.UpdateAt
                    })
                    .ToListAsync();

                return notes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user notes: {ex.Message}", ex);
            }
        }


        public async Task<bool> UpdateUserNote(UserNoteRequestDTO userNoteRequestDTO)
        {
            var oldUserNote = await _context.UserNotes
                .Where(un => un.UserId == userNoteRequestDTO.UserId
                && un.ArticleId == userNoteRequestDTO.ArticleId
                && un.SectionId == userNoteRequestDTO.SectionId)
                .FirstOrDefaultAsync();

            if (oldUserNote == null)
            {
                return false;
            }
            oldUserNote.NoteContent = userNoteRequestDTO.NoteContent;
            oldUserNote.UpdateAt = DateTime.UtcNow;
            int result = await _context.SaveChangesAsync();

            return result > 0;
        }
    }
}
