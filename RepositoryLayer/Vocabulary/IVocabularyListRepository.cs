using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;

public interface IVocabularyListRepository
{
    Task AddAsync(VocabularyList list);
    Task<IEnumerable<VocabularyListDTO>> GetAllAsync(string? searchTerm);
    Task<IEnumerable<VocabularyListDTO>> GetByUserAsync(int userId, string? searchTerm);
    Task<IEnumerable<VocabularyListDTO>> GetPublishedAsync(string? searchTerm);
    Task<VocabularyList?> FindByIdAsync(int id);
    Task<VocabularyList?> UpdateAsync(VocabularyList list);
}