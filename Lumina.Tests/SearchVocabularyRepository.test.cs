using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class SearchVocabularyRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public SearchVocabularyRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTerm_ShouldReturnMatchingVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("Beautiful");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Beautiful", result[0].Word);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermInWord_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("World");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("World", result[0].Word);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermInDefinition_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("Xin chào");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("Xin chào", result[0].Definition);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermInExample_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("how are you");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0].Example);
            Assert.Contains("how are you", result[0].Example, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermInTypeOfWord_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("noun");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 3); // Should find multiple nouns
            Assert.All(result, v => Assert.Equal("noun", v.TypeOfWord, ignoreCase: true));
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermInCategory_ShouldReturnMatchingResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("greeting", result[0].Category, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchAsync_WithListId_ShouldFilterByList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("noun", 1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, v => Assert.Equal(1, v.VocabularyListId));
            Assert.All(result, v => Assert.Equal("noun", v.TypeOfWord, ignoreCase: true));
        }

        [Fact]
        public async Task SearchAsync_WithNullSearchTerm_ShouldReturnAllVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // All vocabularies (excluding deleted)
        }

        [Fact]
        public async Task SearchAsync_WithEmptySearchTerm_ShouldReturnAllVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // All vocabularies
        }

        [Fact]
        public async Task SearchAsync_WithWhitespaceSearchTerm_ShouldReturnAllVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("   ");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // All vocabularies (whitespace is trimmed)
        }

        [Fact]
        public async Task SearchAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result1 = await _repository.SearchAsync("hello");
            var result2 = await _repository.SearchAsync("HELLO");
            var result3 = await _repository.SearchAsync("Hello");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result1.Count, result3.Count);
        }

        [Fact]
        public async Task SearchAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("Deleted");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 6); // VocabularyId 6 is deleted
        }

        [Fact]
        public async Task SearchAsync_ShouldOrderByWord()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("noun");

            // Assert
            Assert.NotNull(result);
            if (result.Count > 1)
            {
                for (int i = 0; i < result.Count - 1; i++)
                {
                    Assert.True(string.Compare(result[i].Word, result[i + 1].Word, StringComparison.Ordinal) <= 0);
                }
            }
        }

        [Fact]
        public async Task SearchAsync_WithNonExistentListId_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("Hello", 999);

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

