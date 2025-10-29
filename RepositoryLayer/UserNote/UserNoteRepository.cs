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

            return await _context.UserNotes
                .Where(un => un.NoteId == userNoteId)
                .Select(un => new UserNoteResponseDTO()
                {
                    NoteId = un.NoteId,
                    UserId = un.UserId,
                    ArticleId = un.ArticleId,
                    SectionId = un.SectionId,
                    NoteContent = un.NoteContent,
                    CreateAt = un.CreateAt,
                    UpdateAt = un.UpdateAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<UserNoteResponseDTO>> GetAllUserNotesByUserId(int userId)
        {
            return await _context.UserNotes
                .AsNoTracking()
                .Where(un => un.UserId == userId)
                .Select(un => new UserNoteResponseDTO()
                {
                    NoteId = un.NoteId,
                    UserId = un.UserId,
                    ArticleId = un.ArticleId,
                    SectionId = un.SectionId,
                    NoteContent = un.NoteContent,
                    CreateAt = un.CreateAt,
                    UpdateAt = un.UpdateAt
                })
                .ToListAsync();
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
