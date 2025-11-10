using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByCategoryRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetByCategoryRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByCategoryAsync_WithValidCategory_ShouldReturnVocabulariesOfThatCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByCategoryAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("greeting", result[0].Category, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("Hello", result[0].Word);
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result1 = await _repository.GetByCategoryAsync("greeting");
            var result2 = await _repository.GetByCategoryAsync("GREETING");
            var result3 = await _repository.GetByCategoryAsync("Greeting");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result1.Count, result3.Count);
        }

        [Fact]
        public async Task GetByCategoryAsync_WithGeneralCategory_ShouldReturnMatchingVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByCategoryAsync("general");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("general", result[0].Category, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetByCategoryAsync_WithNonExistentCategory_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByCategoryAsync("nonexistent");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldExcludeVocabulariesWithNullCategory()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category

            // Act
            var result = await _repository.GetByCategoryAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 5);
            Assert.All(result, v => Assert.NotNull(v.Category));
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 6 is deleted and has category "test"

            // Act
            var result = await _repository.GetByCategoryAsync("test");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 6);
            Assert.All(result, v => Assert.NotEqual(true, v.IsDeleted));
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldOrderByWord()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add another vocabulary with same category
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Hi",
                Definition = "Chào",
                TypeOfWord = "noun",
                Category = "greeting",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByCategoryAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(string.Compare(result[0].Word, result[1].Word, StringComparison.Ordinal) <= 0);
        }

        [Fact]
        public async Task GetByCategoryAsync_WithNullCategory_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByCategoryAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Method filters by Category != null
        }

        [Fact]
        public async Task GetByCategoryAsync_WithEmptyCategory_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByCategoryAsync("");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByCategoryAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetByCategoryAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

