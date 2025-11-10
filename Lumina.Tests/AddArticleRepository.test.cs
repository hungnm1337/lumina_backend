using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer;

namespace Lumina.Tests
{
    public class AddArticleRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly ArticleRepository _repository;

        public AddArticleRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new ArticleRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddArticleToContext()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newArticle = new Article
            {
                Title = "New Article",
                Summary = "New Article Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft"
            };

            // Act
            await _repository.AddAsync(newArticle);
            await _context.SaveChangesAsync();

            // Assert
            var savedArticle = await _context.Articles.FirstOrDefaultAsync(a => a.Title == "New Article");
            Assert.NotNull(savedArticle);
            Assert.Equal("New Article", savedArticle.Title);
            Assert.Equal("New Article Summary", savedArticle.Summary);
        }

        [Fact]
        public async Task AddAsync_ShouldNotSaveUntilSaveChangesCalled()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newArticle = new Article
            {
                Title = "Unsaved Article",
                Summary = "Unsaved Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft"
            };

            // Act
            await _repository.AddAsync(newArticle);
            // Don't call SaveChangesAsync

            // Assert
            var unsavedArticle = await _context.Articles.FirstOrDefaultAsync(a => a.Title == "Unsaved Article");
            Assert.Null(unsavedArticle); // Should not be saved yet
        }

        [Fact]
        public async Task AddSectionsRangeAsync_ShouldAddMultipleSections()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var sections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    ArticleId = 1,
                    SectionTitle = "Section 1",
                    SectionContent = "Content 1",
                    OrderIndex = 1
                },
                new ArticleSection
                {
                    ArticleId = 1,
                    SectionTitle = "Section 2",
                    SectionContent = "Content 2",
                    OrderIndex = 2
                },
                new ArticleSection
                {
                    ArticleId = 1,
                    SectionTitle = "Section 3",
                    SectionContent = "Content 3",
                    OrderIndex = 3
                }
            };

            // Act
            await _repository.AddSectionsRangeAsync(sections);
            await _context.SaveChangesAsync();

            // Assert
            var savedSections = await _context.ArticleSections
                .Where(s => s.ArticleId == 1)
                .ToListAsync();
            Assert.True(savedSections.Count >= 3); // At least 3 new sections (plus existing ones)
            Assert.Contains(savedSections, s => s.SectionTitle == "Section 1");
            Assert.Contains(savedSections, s => s.SectionTitle == "Section 2");
            Assert.Contains(savedSections, s => s.SectionTitle == "Section 3");
        }

        [Fact]
        public async Task AddSectionsRangeAsync_WithEmptyCollection_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var emptySections = new List<ArticleSection>();

            // Act & Assert
            await _repository.AddSectionsRangeAsync(emptySections);
            await _context.SaveChangesAsync();
            // Should not throw exception
            Assert.True(true);
        }

        [Fact]
        public async Task AddSectionsRangeAsync_ShouldNotSaveUntilSaveChangesCalled()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var sections = new List<ArticleSection>
            {
                new ArticleSection
                {
                    ArticleId = 2,
                    SectionTitle = "Unsaved Section",
                    SectionContent = "Unsaved Content",
                    OrderIndex = 10
                }
            };

            // Act
            await _repository.AddSectionsRangeAsync(sections);
            // Don't call SaveChangesAsync

            // Assert
            var unsavedSection = await _context.ArticleSections
                .FirstOrDefaultAsync(s => s.SectionTitle == "Unsaved Section");
            Assert.Null(unsavedSection); // Should not be saved yet
        }

        [Fact]
        public async Task AddAsync_WithAllFields_ShouldSaveAllFields()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedArticleDataAsync(_context);
            var newArticle = new Article
            {
                Title = "Complete Article",
                Summary = "Complete Summary",
                CategoryId = 2,
                CreatedBy = 2,
                UpdatedBy = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublished = true,
                Status = "Published",
                RejectionReason = null
            };

            // Act
            await _repository.AddAsync(newArticle);
            await _context.SaveChangesAsync();

            // Assert
            var savedArticle = await _context.Articles.FirstOrDefaultAsync(a => a.Title == "Complete Article");
            Assert.NotNull(savedArticle);
            Assert.Equal("Complete Article", savedArticle.Title);
            Assert.Equal("Complete Summary", savedArticle.Summary);
            Assert.Equal(2, savedArticle.CategoryId);
            Assert.Equal(2, savedArticle.CreatedBy);
            Assert.Equal(2, savedArticle.UpdatedBy);
            Assert.True(savedArticle.IsPublished);
            Assert.Equal("Published", savedArticle.Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

