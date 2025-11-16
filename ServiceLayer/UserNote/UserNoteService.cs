using DataLayer.DTOs.UserNote;
using RepositoryLayer.UserNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ServiceLayer.UserNote
{
    public class UserNoteService : IUserNoteService
    {
        private readonly IUserNoteRepository _userNoteRepository;

        public UserNoteService(IUserNoteRepository userNoteRepository)
        {
            _userNoteRepository = userNoteRepository;
        }

        public async Task<IEnumerable<UserNoteResponseDTO>> GetAllUserNotesByUserId(int userId)
        {
            return await _userNoteRepository.GetAllUserNotesByUserId(userId);
        }

        public async Task<UserNoteResponseDTO> GetUserNoteByID(int userNoteId)
        {
            return await _userNoteRepository.GetUserNoteByID(userNoteId);
        }

        public async Task<UserNoteResponseDTO> GetUserNoteByUserIDAndArticleId(int userId, int articleId, int sectionId)
        {
            return await _userNoteRepository.GetUserNoteByUserIDAndArticleId( userId,  articleId, sectionId);
        }

        public async Task<bool> UpsertUserNote(UserNoteRequestDTO userNoteRequestDTO)
        {
            if(await _userNoteRepository.CheckUserNoteExist(
                userNoteRequestDTO.UserId,
                userNoteRequestDTO.ArticleId,
                userNoteRequestDTO.SectionId))
            {
                return await _userNoteRepository.UpdateUserNote(userNoteRequestDTO);
            }
            else
            {
                return await _userNoteRepository.AddNewUserNote(userNoteRequestDTO);
            }
        }
    }
}
