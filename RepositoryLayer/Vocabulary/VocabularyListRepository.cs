using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

public class VocabularyListRepository : IVocabularyListRepository
{
    private readonly LuminaSystemContext _context;

    public VocabularyListRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VocabularyList list)
    {
        await _context.VocabularyLists.AddAsync(list);
    }

    public async Task<VocabularyList?> FindByIdAsync(int id)
    {
        return await _context.VocabularyLists.FindAsync(id);
    }

    public async Task<IEnumerable<VocabularyListDTO>> GetAllAsync(string? searchTerm)
    {
        var query = _context.VocabularyLists
            .Where(vl => vl.IsDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(vl => vl.Name.Contains(searchTerm));
        }

        return await query
            .OrderByDescending(vl => vl.CreateAt)
            .Select(vl => new VocabularyListDTO
            {
                VocabularyListId = vl.VocabularyListId,
                Name = vl.Name,
                IsPublic = vl.IsPublic,
                MakeByName = vl.MakeByNavigation.FullName, // Lấy tên người tạo
                CreateAt = vl.CreateAt,
                VocabularyCount = vl.Vocabularies.Count(v => v.IsDeleted != true) // Đếm số từ
            })
            .ToListAsync();
    }
}