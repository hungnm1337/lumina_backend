using DataLayer.DTOs.Vocabulary;

namespace ServiceLayer.Vocabulary
{
    public interface IVocabularyListService
    {
        Task<VocabularyListDTO> CreateListAsync(VocabularyListCreateDTO dto, int creatorUserId);
        Task<IEnumerable<VocabularyListDTO>> GetListsAsync(string? searchTerm);
    }
}