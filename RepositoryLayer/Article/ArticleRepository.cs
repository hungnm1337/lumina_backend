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
}