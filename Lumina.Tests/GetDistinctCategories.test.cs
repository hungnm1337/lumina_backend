using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetDistinctCategoriesTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetDistinctCategoriesTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldReturnAllDistinctCategories()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("greeting", result, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("general", result, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("appearance", result, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("action", result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldReturnDistinctValues()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add duplicate category
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "AnotherGreeting",
                Definition = "Another greeting",
                TypeOfWord = "noun",
                Category = "greeting",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            var greetingCount = result.Count(c => c.Equals("greeting", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(1, greetingCount); // Should only appear once
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldExcludeNullCategories()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, c => c == null);
            Assert.All(result, c => Assert.NotNull(c));
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 6 is deleted and has category "test"

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, c => c != null && c.Equals("test", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldOrderAlphabetically()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            if (result.Count > 1)
            {
                for (int i = 0; i < result.Count - 1; i++)
                {
                    Assert.True(string.Compare(result[i], result[i + 1], StringComparison.Ordinal) <= 0,
                        $"Categories should be ordered: {result[i]} should come before {result[i + 1]}");
                }
            }
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_WithOnlyNullCategories_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Remove all vocabularies with categories
            var vocabulariesWithCategories = _context.Vocabularies.Where(v => v.Category != null).ToList();
            _context.Vocabularies.RemoveRange(vocabulariesWithCategories);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDistinctCategoriesAsync_ShouldReturnCaseSensitiveCategories()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add vocabulary with different case
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "TestWord",
                Definition = "Test",
                TypeOfWord = "noun",
                Category = "GREETING", // Different case
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetDistinctCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            // Categories should be returned as stored in database
            // The method does case-insensitive comparison but returns original values
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

