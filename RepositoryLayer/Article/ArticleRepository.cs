using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer;

public class ArticleRepository : IArticleRepository
{
    private readonly LuminaSystemContext _context;

    public ArticleRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Article article)
    {
        await _context.Articles.AddAsync(article);
    }

    public async Task AddSectionsRangeAsync(IEnumerable<ArticleSection> sections)
    {
        await _context.ArticleSections.AddRangeAsync(sections);
    }

    public async Task<List<Article>> GetAllWithCategoryAndUserAsync()
    {
        return await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.CreatedByNavigation)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Article?> FindByIdAsync(int id)
    {
        return await _context.Articles
            .Include(a => a.ArticleSections)
            .FirstOrDefaultAsync(a => a.ArticleId == id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _context.Articles
            .Include(a => a.ArticleSections)
            .FirstOrDefaultAsync(a => a.ArticleId == id);

        if (article != null)
        {
            // Xóa sections trước
            _context.ArticleSections.RemoveRange(article.ArticleSections);
            // Xóa article
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<Article?> UpdateAsync(Article article)
    {
        _context.Articles.Update(article);
        await _context.SaveChangesAsync();
        return article;
    }

    public async Task<(List<Article> items, int total)> QueryAsync(int page, int pageSize, string? search, int? categoryId, bool? isPublished, string sortBy, string sortDir)
    {
        var query = _context.Articles
            .Include(a => a.Category)
            .Include(a => a.CreatedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(s) || a.Summary.ToLower().Contains(s) || a.ArticleSections.Any(sec => sec.SectionContent.ToLower().Contains(s)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        if (isPublished.HasValue)
        {
            query = query.Where(a => a.IsPublished == isPublished.Value);
        }

        // Sorting
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLower()) switch
        {
            "title" => desc ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "category" => desc ? query.OrderByDescending(a => a.Category.CategoryName) : query.OrderBy(a => a.Category.CategoryName),
            _ => desc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}