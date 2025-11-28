using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer;

public class CategoryRepository : ICategoryRepository
{
    private readonly LuminaSystemContext _context;

    public CategoryRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<ArticleCategory?> FindByIdAsync(int id)
    {
        return await _context.ArticleCategories.FindAsync(id);
    }

    public async Task<List<ArticleCategory>> GetAllAsync()
    {
        return await Task.FromResult(_context.ArticleCategories.OrderBy(c => c.CategoryName).ToList());
    }

    public async Task<ArticleCategory> AddAsync(ArticleCategory category)
    {
        await _context.ArticleCategories.AddAsync(category);
        return category;
    }

    public async Task<bool> ExistsByNameAsync(string categoryName)
    {
        return await _context.ArticleCategories
            .AnyAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());
    }
}