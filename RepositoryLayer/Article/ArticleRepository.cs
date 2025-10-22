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

    public async Task<(List<Article> items, int total)> QueryAsync(int page, int pageSize, string? search, int? categoryId, bool? isPublished, string? status, string sortBy, string sortDir, int? createdBy = null)
    {
        var query = _context.Articles
            .Include(a => a.Category)
            .Include(a => a.CreatedByNavigation)
            .Include(a => a.ArticleSections)
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

        // Giờ đây 'status' đã hợp lệ
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        // Filter by author/user ID
        if (createdBy.HasValue)
        {
            query = query.Where(a => a.CreatedBy == createdBy.Value);
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
    public async Task UpdateSectionsAsync(int articleId, IEnumerable<ArticleSection> newSections)
    {
        // Bắt đầu một transaction để đảm bảo tất cả các thao tác đều thành công hoặc thất bại cùng nhau.
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Tìm và xóa tất cả các section cũ thuộc về bài viết này.
            var existingSections = await _context.ArticleSections
                .Where(s => s.ArticleId == articleId)
                .ToListAsync();

            if (existingSections.Any())
            {
                _context.ArticleSections.RemoveRange(existingSections);
            }

            // 2. Thêm các section mới được cung cấp.
            // Cần đảm bảo rằng ArticleId được gán chính xác cho mỗi section mới.
            var sectionsToAdd = newSections.Select(s => new ArticleSection
            {
                ArticleId = articleId, // Gán ID của bài viết cha
                SectionTitle = s.SectionTitle,
                SectionContent = s.SectionContent,
                OrderIndex = s.OrderIndex
            }).ToList();

            if (sectionsToAdd.Any())
            {
                await _context.ArticleSections.AddRangeAsync(sectionsToAdd);
            }

            // 3. Lưu tất cả các thay đổi (xóa và thêm) vào cơ sở dữ liệu.
            await _context.SaveChangesAsync();

            // 4. Nếu mọi thứ thành công, commit transaction.
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            // Nếu có bất kỳ lỗi nào xảy ra, rollback transaction để cơ sở dữ liệu trở lại trạng thái ban đầu.
            await transaction.RollbackAsync();
            // Ném lại lỗi để lớp gọi có thể xử lý.
            throw;
        }
    }
}