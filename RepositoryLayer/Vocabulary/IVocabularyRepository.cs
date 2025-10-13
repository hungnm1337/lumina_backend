using DataLayer.Models;

public interface IVocabularyRepository
{
    Task<List<Vocabulary>> GetByListAsync(int? vocabularyListId, string? search);
    Task<Vocabulary?> GetByIdAsync(int id);
    Task AddAsync(Vocabulary vocab);
    Task UpdateAsync(Vocabulary vocab);
    Task DeleteAsync(int id);
    Task<Dictionary<int, int>> GetCountsByListAsync();
    Task<List<Vocabulary>> SearchAsync(string searchTerm, int? listId = null);
    Task<int> GetTotalCountAsync();
    Task<List<Vocabulary>> GetByTypeAsync(string typeOfWord);
    Task<List<Vocabulary>> GetByCategoryAsync(string category);
    Task<List<string>> GetDistinctCategoriesAsync();
}


