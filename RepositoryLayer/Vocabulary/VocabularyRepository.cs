using DataLayer.Models;


public class VocabularyRepository : IVocabularyRepository
{
    private readonly LuminaSystemContext _context;

    public VocabularyRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<List<Vocabulary>> GetByListAsync(int? vocabularyListId, string? search)
    {
        var query = _context.Vocabularies.AsQueryable();
        if (vocabularyListId.HasValue)
        {
            query = query.Where(v => v.VocabularyListId == vocabularyListId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(v => v.Word.Contains(s) || v.Definition.Contains(s));
        }
        return await Task.FromResult(query.OrderBy(v => v.Word).ToList());
    }

    public async Task AddAsync(Vocabulary vocab)
    {
        await _context.Vocabularies.AddAsync(vocab);
    }

    public async Task<Dictionary<int, int>> GetCountsByListAsync()
    {
        return await Task.FromResult(
            _context.Vocabularies
                .GroupBy(v => v.VocabularyListId)
                .ToDictionary(g => g.Key, g => g.Count())
        );
    }
}


