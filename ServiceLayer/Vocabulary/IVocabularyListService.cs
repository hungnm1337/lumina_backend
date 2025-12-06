using DataLayer.DTOs.Vocabulary;

namespace ServiceLayer.Vocabulary
{
    public interface IVocabularyListService
    {
        Task<VocabularyListDTO> CreateListAsync(VocabularyListCreateDTO dto, int creatorUserId);
        Task<IEnumerable<VocabularyListDTO>> GetListsAsync(string? searchTerm);
        Task<IEnumerable<VocabularyListDTO>> GetListsByUserAsync(int userId, string? searchTerm);
        Task<IEnumerable<VocabularyListDTO>> GetPublishedListsAsync(string? searchTerm);
        Task<IEnumerable<VocabularyListDTO>> GetMyAndStaffListsAsync(int userId, string? searchTerm);
        Task<bool> RequestApprovalAsync(int listId, int staffUserId);
        Task<bool> ReviewListAsync(int listId, bool isApproved, string? comment, int managerUserId);
        Task<bool> DeleteListAsync(int listId, int userId);
    }
}