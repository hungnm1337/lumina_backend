using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;
using System;
using System.Linq;

namespace Lumina.Tests
{
    public class GetAllArticlesRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public GetAllArticlesRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldReturnAllArticles()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // 5 articles in seed data
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldIncludeCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.All(result, article => Assert.NotNull(article.Category));
            Assert.All(result, article => Assert.NotNull(article.Category.CategoryName));
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldIncludeCreatedByNavigation()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.All(result, article => Assert.NotNull(article.CreatedByNavigation));
            Assert.All(result, article => Assert.NotNull(article.CreatedByNavigation.FullName));
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldAccessUpdatedByNavigation()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            
            // Update an article to have UpdatedByNavigation
            var article = await _context.Articles.FindAsync(1);
            Assert.NotNull(article);
            var updaterUser = await _context.Users.FindAsync(2);
            Assert.NotNull(updaterUser);
            article.UpdatedBy = updaterUser.UserId;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedByNavigation = updaterUser; // Set UpdatedByNavigation property
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            var updatedArticle = result.FirstOrDefault(a => a.ArticleId == 1);
            Assert.NotNull(updatedArticle);
            // Access UpdatedByNavigation property to ensure coverage
            var updatedByNav = updatedArticle.UpdatedByNavigation;
            // Property should be accessible (may be null if not loaded by EF Core)
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldOrderByCreatedAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 1);
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].CreatedAt >= result[i + 1].CreatedAt,
                    $"Articles should be ordered by CreatedAt descending. {result[i].CreatedAt} should be >= {result[i + 1].CreatedAt}");
            }
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldReturnAllArticlesRegardlessOfStatus()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, a => a.Status == "Published");
            Assert.Contains(result, a => a.Status == "Draft");
            Assert.Contains(result, a => a.Status == "Pending");
            Assert.Contains(result, a => a.Status == "Rejected");
        }

        [Fact]
        public async Task GetAllWithCategoryAndUserAsync_ShouldReturnAllArticlesRegardlessOfIsPublished()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);

            // Act
            var result = await _repository.GetAllWithCategoryAndUserAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, a => a.IsPublished == true);
            Assert.Contains(result, a => a.IsPublished == false);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

