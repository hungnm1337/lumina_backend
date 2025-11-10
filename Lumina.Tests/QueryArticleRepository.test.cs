using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;

namespace Lumina.Tests
{
    public class QueryArticleRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public QueryArticleRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        #region Basic Query Tests

        [Fact]
        public async Task QueryAsync_WithNoFilters_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total); // 5 articles in seed data
            Assert.True(items.Count <= 10);
        }

        [Fact]
        public async Task QueryAsync_ShouldIncludeCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article => Assert.NotNull(article.Category));
        }

        [Fact]
        public async Task QueryAsync_ShouldIncludeCreatedByNavigation()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article => Assert.NotNull(article.CreatedByNavigation));
        }

        [Fact]
        public async Task QueryAsync_ShouldIncludeArticleSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article => Assert.NotNull(article.ArticleSections));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task QueryAsync_WithSearchTerm_ShouldFilterByTitle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "Published", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 2); // At least 2 articles with "Published" in title
            Assert.All(items, article => Assert.Contains("Published", article.Title, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task QueryAsync_WithSearchTerm_ShouldFilterBySummary()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "published article", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => 
                Assert.True(
                    article.Title.Contains("published", StringComparison.OrdinalIgnoreCase) ||
                    article.Summary.Contains("published", StringComparison.OrdinalIgnoreCase) ||
                    article.ArticleSections.Any(s => s.SectionContent.Contains("published", StringComparison.OrdinalIgnoreCase))
                ));
        }

        [Fact]
        public async Task QueryAsync_WithSearchTerm_ShouldFilterBySectionContent()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "introduction", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => 
                Assert.True(
                    article.Title.Contains("introduction", StringComparison.OrdinalIgnoreCase) ||
                    article.Summary.Contains("introduction", StringComparison.OrdinalIgnoreCase) ||
                    article.ArticleSections.Any(s => s.SectionContent.Contains("introduction", StringComparison.OrdinalIgnoreCase))
                ));
        }

        [Fact]
        public async Task QueryAsync_WithNullSearchTerm_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        [Fact]
        public async Task QueryAsync_WithEmptySearchTerm_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        [Fact]
        public async Task QueryAsync_WithWhitespaceSearchTerm_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "   ", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermNotMatching_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "NonexistentSearchTerm12345", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        #endregion

        #region CategoryId Filter Tests

        [Fact]
        public async Task QueryAsync_WithCategoryId_ShouldFilterByCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, 1, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal(1, article.CategoryId));
        }

        [Fact]
        public async Task QueryAsync_WithCategoryId_ShouldReturnOnlyMatchingCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, 2, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal(2, article.CategoryId));
        }

        [Fact]
        public async Task QueryAsync_WithNonExistentCategoryId_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, 999, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task QueryAsync_WithNullCategoryId_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        #endregion

        #region IsPublished Filter Tests

        [Fact]
        public async Task QueryAsync_WithIsPublishedTrue_ShouldReturnOnlyPublishedArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, true, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 2);
            Assert.All(items, article => Assert.True(article.IsPublished == true));
        }

        [Fact]
        public async Task QueryAsync_WithIsPublishedFalse_ShouldReturnOnlyUnpublishedArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, false, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 3);
            Assert.All(items, article => Assert.True(article.IsPublished == false));
        }

        [Fact]
        public async Task QueryAsync_WithNullIsPublished_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        #endregion

        #region Status Filter Tests

        [Fact]
        public async Task QueryAsync_WithStatus_ShouldFilterByStatus()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "Published", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 2);
            Assert.All(items, article => Assert.Equal("Published", article.Status));
        }

        [Fact]
        public async Task QueryAsync_WithDraftStatus_ShouldReturnOnlyDraftArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "Draft", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal("Draft", article.Status));
        }

        [Fact]
        public async Task QueryAsync_WithPendingStatus_ShouldReturnOnlyPendingArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "Pending", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal("Pending", article.Status));
        }

        [Fact]
        public async Task QueryAsync_WithRejectedStatus_ShouldReturnOnlyRejectedArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "Rejected", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal("Rejected", article.Status));
        }

        [Fact]
        public async Task QueryAsync_WithNullStatus_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        [Fact]
        public async Task QueryAsync_WithEmptyStatus_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        #endregion

        #region CreatedBy Filter Tests

        [Fact]
        public async Task QueryAsync_WithCreatedBy_ShouldFilterByAuthor()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", 1);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 4);
            Assert.All(items, article => Assert.Equal(1, article.CreatedBy));
        }

        [Fact]
        public async Task QueryAsync_WithCreatedBy_ShouldReturnOnlyAuthorArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", 3);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.All(items, article => Assert.Equal(3, article.CreatedBy));
        }

        [Fact]
        public async Task QueryAsync_WithNonExistentCreatedBy_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", 999);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task QueryAsync_WithNullCreatedBy_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task QueryAsync_WithSortByCreatedAtDesc_ShouldOrderByCreatedAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt >= items[i + 1].CreatedAt,
                        $"Articles should be ordered by CreatedAt descending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortByCreatedAtAsc_ShouldOrderByCreatedAtAscending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "asc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt <= items[i + 1].CreatedAt,
                        $"Articles should be ordered by CreatedAt ascending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortByTitleDesc_ShouldOrderByTitleDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "title", "desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(string.Compare(items[i].Title, items[i + 1].Title, StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Articles should be ordered by Title descending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortByTitleAsc_ShouldOrderByTitleAscending()
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
                    Assert.True(string.Compare(items[i].Title, items[i + 1].Title, StringComparison.OrdinalIgnoreCase) <= 0,
                        $"Articles should be ordered by Title ascending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortByCategoryDesc_ShouldOrderByCategoryNameDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "category", "desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(string.Compare(items[i].Category.CategoryName, items[i + 1].Category.CategoryName, StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Articles should be ordered by Category descending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithSortByCategoryAsc_ShouldOrderByCategoryNameAscending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "category", "asc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(string.Compare(items[i].Category.CategoryName, items[i + 1].Category.CategoryName, StringComparison.OrdinalIgnoreCase) <= 0,
                        $"Articles should be ordered by Category ascending");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithInvalidSortBy_ShouldDefaultToCreatedAt()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "invalidSort", "desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt >= items[i + 1].CreatedAt,
                        $"Articles should default to CreatedAt ordering");
                }
            }
        }

        [Fact]
        public async Task QueryAsync_WithNullSortBy_ShouldDefaultToCreatedAt()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, null!, "desc", null);

            // Assert
            Assert.NotNull(items);
            if (items.Count > 1)
            {
                for (int i = 0; i < items.Count - 1; i++)
                {
                    Assert.True(items[i].CreatedAt >= items[i + 1].CreatedAt,
                        $"Articles should default to CreatedAt ordering");
                }
            }
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 2, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total); // Total count
            Assert.True(items.Count <= 2); // Page size
        }

        [Fact]
        public async Task QueryAsync_WithPage2_ShouldReturnSecondPage()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (itemsPage1, total) = await _repository.QueryAsync(1, 2, null, null, null, null, "createdAt", "desc", null);
            var (itemsPage2, _) = await _repository.QueryAsync(2, 2, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(itemsPage1);
            Assert.NotNull(itemsPage2);
            Assert.Equal(5, total);
            Assert.True(itemsPage1.Count <= 2);
            Assert.True(itemsPage2.Count <= 2);
            // Verify pages don't overlap
            var page1Ids = itemsPage1.Select(a => a.ArticleId).ToList();
            var page2Ids = itemsPage2.Select(a => a.ArticleId).ToList();
            Assert.Empty(page1Ids.Intersect(page2Ids));
        }

        [Fact]
        public async Task QueryAsync_WithPageBeyondResults_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(100, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task QueryAsync_WithLargePageSize_ShouldReturnAllResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 100, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(5, total);
            Assert.Equal(5, items.Count);
        }

        #endregion

        #region Combined Filter Tests

        [Fact]
        public async Task QueryAsync_WithMultipleFilters_ShouldApplyAllFilters()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "Article", 1, true, "Published", "createdAt", "desc", 1);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article =>
            {
                Assert.Contains("Article", article.Title, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(1, article.CategoryId);
                Assert.True(article.IsPublished == true);
                Assert.Equal("Published", article.Status);
                Assert.Equal(1, article.CreatedBy);
            });
        }

        [Fact]
        public async Task QueryAsync_WithSearchAndCategory_ShouldFilterByBoth()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "Published", 1, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article =>
            {
                Assert.Contains("Published", article.Title, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(1, article.CategoryId);
            });
        }

        [Fact]
        public async Task QueryAsync_WithStatusAndIsPublished_ShouldFilterByBoth()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, true, "Published", "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article =>
            {
                Assert.True(article.IsPublished == true);
                Assert.Equal("Published", article.Status);
            });
        }

        [Fact]
        public async Task QueryAsync_WithCreatedByAndStatus_ShouldFilterByBoth()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, "Published", "createdAt", "desc", 1);

            // Assert
            Assert.NotNull(items);
            Assert.All(items, article =>
            {
                Assert.Equal("Published", article.Status);
                Assert.Equal(1, article.CreatedBy);
            });
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task QueryAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task QueryAsync_WithSearchTermInSectionContent_ShouldFindArticle()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            // Article 1 has section with content "This is the introduction section"

            // Act
            var (items, total) = await _repository.QueryAsync(1, 10, "introduction section", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items);
            Assert.True(total >= 1);
            Assert.Contains(items, article => article.ArticleSections.Any(s => 
                s.SectionContent.Contains("introduction", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task QueryAsync_WithCaseInsensitiveSearch_ShouldMatch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var (items1, _) = await _repository.QueryAsync(1, 10, "published", null, null, null, "createdAt", "desc", null);
            var (items2, _) = await _repository.QueryAsync(1, 10, "PUBLISHED", null, null, null, "createdAt", "desc", null);
            var (items3, _) = await _repository.QueryAsync(1, 10, "Published", null, null, null, "createdAt", "desc", null);

            // Assert
            Assert.NotNull(items1);
            Assert.NotNull(items2);
            Assert.NotNull(items3);
            Assert.Equal(items1.Count, items2.Count);
            Assert.Equal(items1.Count, items3.Count);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

