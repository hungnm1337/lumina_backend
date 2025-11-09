using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetByListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByListAsync_WithListId_ShouldReturnVocabulariesForThatList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count); // 4 vocabularies in list 1 (excluding deleted)
            Assert.All(result, v => Assert.Equal(1, v.VocabularyListId));
            Assert.All(result, v => Assert.NotEqual(true, v.IsDeleted));
            // Verify ordering by Word
            Assert.Equal("Beautiful", result[0].Word);
            Assert.Equal("Hello", result[1].Word);
            Assert.Equal("Test", result[2].Word);
            Assert.Equal("World", result[3].Word);
        }

        [Fact]
        public async Task GetByListAsync_WithNullListId_ShouldReturnAllVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // All vocabularies from both lists (excluding deleted)
            Assert.All(result, v => Assert.NotEqual(true, v.IsDeleted));
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(null, "Hello");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Word);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermInDefinition_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(null, "Xin chào");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Word);
            Assert.Contains("Xin chào", result[0].Definition);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermInCategory_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(null, "greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Word);
            Assert.Equal("greeting", result[0].Category);
        }

        [Fact]
        public async Task GetByListAsync_WithListIdAndSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(1, "World");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("World", result[0].Word);
            Assert.Equal(1, result[0].VocabularyListId);
        }

        [Fact]
        public async Task GetByListAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 6); // VocabularyId 6 is deleted
            Assert.All(result, v => Assert.NotEqual(true, v.IsDeleted));
        }

        [Fact]
        public async Task GetByListAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetByListAsync(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByListAsync_WithNonExistentListId_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(999, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByListAsync_WithWhitespaceSearch_ShouldReturnAllResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(null, "   ");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // All vocabularies (whitespace is trimmed)
        }

        [Fact]
        public async Task GetByListAsync_ShouldOrderByWord()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByListAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 1);
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(string.Compare(result[i].Word, result[i + 1].Word, StringComparison.Ordinal) <= 0,
                    $"Words should be ordered: {result[i].Word} should come before {result[i + 1].Word}");
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

