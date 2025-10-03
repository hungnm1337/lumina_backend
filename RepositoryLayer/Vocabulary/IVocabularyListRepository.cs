using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;

public interface IVocabularyListRepository
{
    Task AddAsync(VocabularyList list);
    Task<IEnumerable<VocabularyListDTO>> GetAllAsync(string? searchTerm);
    Task<VocabularyList?> FindByIdAsync(int id);
}