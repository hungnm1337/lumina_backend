using DataLayer.Models;


public interface IVocabularyRepository
{
    Task<List<Vocabulary>> GetByListAsync(int? vocabularyListId, string? search);
    Task AddAsync(Vocabulary vocab);
    Task<Dictionary<int, int>> GetCountsByListAsync();
}


