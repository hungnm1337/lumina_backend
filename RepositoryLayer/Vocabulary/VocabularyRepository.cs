using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

public class VocabularyRepository : IVocabularyRepository
{
    private readonly LuminaSystemContext _context;

    public VocabularyRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<List<Vocabulary>> GetByListAsync(int? vocabularyListId, string? search)
    {
        var query = _context.Vocabularies.Where(v => v.IsDeleted != true).AsQueryable();
        if (vocabularyListId.HasValue)
        {
            query = query.Where(v => v.VocabularyListId == vocabularyListId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(v => v.Word.Contains(s) || v.Definition.Contains(s));
        }
        return await query.OrderBy(v => v.Word).ToListAsync();
    }

    public async Task<Vocabulary?> GetByIdAsync(int id)
    {
        return await _context.Vocabularies
            .Where(v => v.VocabularyId == id && v.IsDeleted != true)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Vocabulary vocab)
    {
        await _context.Vocabularies.AddAsync(vocab);
    }

    public async Task UpdateAsync(Vocabulary vocab)
    {
        _context.Vocabularies.Update(vocab);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var vocab = await _context.Vocabularies.FindAsync(id);
        if (vocab != null)
        {
            vocab.IsDeleted = true; // Soft delete
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<int, int>> GetCountsByListAsync()
    {
        return await _context.Vocabularies
            .Where(v => v.IsDeleted != true)
            .GroupBy(v => v.VocabularyListId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<List<Vocabulary>> SearchAsync(string searchTerm, int? listId = null)
    {
        var query = _context.Vocabularies
            .Where(v => v.IsDeleted != true)
            .AsQueryable();

        if (listId.HasValue)
        {
            query = query.Where(v => v.VocabularyListId == listId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(v => 
                v.Word.ToLower().Contains(term) || 
                v.Definition.ToLower().Contains(term) ||
                (v.Example != null && v.Example.ToLower().Contains(term)) ||
                v.TypeOfWord.ToLower().Contains(term));
        }

        return await query.OrderBy(v => v.Word).ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Vocabularies
            .Where(v => v.IsDeleted != true)
            .CountAsync();
    }

    public async Task<List<Vocabulary>> GetByTypeAsync(string typeOfWord)
    {
        return await _context.Vocabularies
            .Where(v => v.IsDeleted != true && v.TypeOfWord.ToLower() == typeOfWord.ToLower())
            .OrderBy(v => v.Word)
            .ToListAsync();
    }
}


