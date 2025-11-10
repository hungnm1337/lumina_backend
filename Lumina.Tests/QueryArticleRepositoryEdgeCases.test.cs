using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;

namespace Lumina.Tests
{
    public class QueryArticleRepositoryEdgeCasesTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public QueryArticleRepositoryEdgeCasesTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        [Fact]
        public async Task QueryAsync_WithSortDirCaseInsensitive_ShouldHandleUpperCase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "DESC", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt >= items[i + 1].CreatedAt);
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortDirCaseInsensitive_ShouldHandleMixedCase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "Desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt >= items[i + 1].CreatedAt);
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortDirAsc_ShouldOrderAscending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "title", "asc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(string.Compare(items[i].Title, items[i + 1].Title, StringComparison.OrdinalIgnoreCase) <= 0);
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortDirNotDesc_ShouldOrderAscending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "title", "ascending", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(string.Compare(items[i].Title, items[i + 1].Title, StringComparison.OrdinalIgnoreCase) <= 0);
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermInMultipleFields_ShouldFindArticle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Article 1 has Title "Published Article", Summary "This is a published article", and sections

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "published", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermMatchingTitleOnly_ShouldReturnArticle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "Draft", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => 
                Assert.True(
                    article.Title.Contains("Draft", StringComparison.OrdinalIgnoreCase) ||
                    article.Summary.Contains("Draft", StringComparison.OrdinalIgnoreCase) ||
                    article.ArticleSections.Any(s => s.SectionContent.Contains("Draft", StringComparison.OrdinalIgnoreCase))
                ));
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermMatchingSummaryOnly_ShouldReturnArticle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "This is a", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
        }

        [Fact]
        public async Task QueryAsync_WithAllFiltersCombined_ShouldApplyAllCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act - Search for "Article", Category 1, Published, Status "Published", CreatedBy 1
            var (items, total) = await _repository.QueryAsync(1, 10, "Article", 1, true, "Published", "title", "asc", 1);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article =>
            {
                Assert.True(
                    article.Title.Contains("Article", StringComparison.OrdinalIgnoreCase) ||
                    article.Summary.Contains("Article", StringComparison.OrdinalIgnoreCase) ||
                    article.ArticleSections.Any(s => s.SectionContent.Contains("Article", StringComparison.OrdinalIgnoreCase))
                );
                Assert.Equal(1, article.CategoryId);
                Assert.True(article.IsPublished == true);
                Assert.Equal("Published", article.Status);
                Assert.Equal(1, article.CreatedBy);
            });
        }

        [Fact]
        public async Task QueryAsync_WithPageSizeZero_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 0, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total); // Total should still be correct
            Assert.Empty(items); // But no items returned due to pageSize 0
        }

        [Fact]
        public async Task QueryAsync_WithPageOne_ShouldReturnFirstPage()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 3, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
            Assert.True(items.Count <= 3);
        }

        [Fact]
        public async Task QueryAsync_WithPageTwo_ShouldReturnSecondPage()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(2, 3, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
            Assert.True(items.Count <= 3);
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermTrimmed_ShouldWorkCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items1, _) = await _repository.QueryAsync(1, 10, "  Published  ", null, null, null, "createdAt", "desc", null);
            var (items2, _) = await _repository.QueryAsync(1, 10, "Published", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items1);
            Assert.NotNull(items2);
            Assert.Equal(items1.Count, items2.Count);
        }

        [Fact]
        public async Task QueryAsync_WithCategoryHavingNoArticles_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Create a category with no articles
            var emptyCategory = new ArticleCategory
            {
                CategoryId = 99,
                CategoryName = "Empty Category",
                Description = "Empty",
                CreatedByUserId = 1,
                CreateAt = DateTime.UtcNow
            };
            _context.ArticleCategories.Add(emptyCategory);
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, 99, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

