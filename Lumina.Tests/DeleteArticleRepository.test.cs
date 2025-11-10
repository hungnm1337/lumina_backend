using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;

namespace Lumina.Tests
{
    public class DeleteArticleRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public DeleteArticleRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var articleBeforeDelete = await _context.Articles.FindAsync(1);
            Assert.NotNull(articleBeforeDelete);

            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            var articleAfterDelete = await _context.Articles.FindAsync(1);
            Assert.Null(articleAfterDelete);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteArticleSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var sectionsBeforeDelete = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.NotEmpty(sectionsBeforeDelete);

            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            var sectionsAfterDelete = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.Empty(sectionsAfterDelete);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_WithArticleHavingNoSections_ShouldStillDeleteArticle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Article 3 may have no sections
            var articleBeforeDelete = await _context.Articles.FindAsync(3);
            Assert.NotNull(articleBeforeDelete);

            // Act
            var result = await _repository.DeleteAsync(3);

            // Assert
            Assert.True(result);
            var articleAfterDelete = await _context.Articles.FindAsync(3);
            Assert.Null(articleAfterDelete);
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotDeleteOtherArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var totalArticlesBefore = await _context.Articles.CountAsync();

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            var totalArticlesAfter = await _context.Articles.CountAsync();
            Assert.Equal(totalArticlesBefore - 1, totalArticlesAfter);
            
            // Verify other articles still exist
            var article2 = await _context.Articles.FindAsync(2);
            Assert.NotNull(article2);
            var article3 = await _context.Articles.FindAsync(3);
            Assert.NotNull(article3);
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotDeleteOtherArticleSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var article2SectionsBefore = await _context.ArticleSections
                .Where(s => s.ArticleId == 2)
                .ToListAsync();

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            var article2SectionsAfter = await _context.ArticleSections
                .Where(s => s.ArticleId == 2)
                .ToListAsync();
            Assert.Equal(article2SectionsBefore.Count, article2SectionsAfter.Count);
        }

        [Fact]
        public async Task DeleteAsync_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var article = await _context.Articles.FindAsync(1);
            Assert.NotNull(article);

            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            // Verify deletion is persisted by querying again
            var deletedArticle = await _context.Articles.FindAsync(1);
            Assert.Null(deletedArticle);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

