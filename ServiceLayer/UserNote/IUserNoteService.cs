using DataLayer.DTOs.UserNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.UserNote
{
    public interface IUserNoteService
    {
        public Task<bool> UpsertUserNote(UserNoteRequestDTO userNoteRequestDTO);

        public Task<IEnumerable<UserNoteResponseDTO>> GetAllUserNotesByUserId(int userId);

        public Task<UserNoteResponseDTO> GetUserNoteByID(int userNoteId);

        public Task<UserNoteResponseDTO> GetUserNoteByUserIDAndArticleId(int userId, int articleId);
    }
}
