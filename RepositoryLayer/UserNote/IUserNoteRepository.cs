using DataLayer.DTOs.UserNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.UserNote
{
    public interface IUserNoteRepository
    {
        public Task<bool> AddNewUserNote(UserNoteRequestDTO userNoteRequestDTO);
        public Task<IEnumerable<UserNoteResponseDTO>> GetAllUserNotesByUserId(int userId);

        public Task<bool> UpdateUserNote(UserNoteRequestDTO userNoteRequestDTO);

        public Task<UserNoteResponseDTO> GetUserNoteByID(int userNoteId);

        public Task<bool> CheckUserNoteExist(int userId, int articleId, int sectionId);
    }
}
