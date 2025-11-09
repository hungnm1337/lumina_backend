using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByTypeRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetByTypeRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByTypeAsync_WithValidType_ShouldReturnVocabulariesOfThatType()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("noun");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 3); // At least 3 nouns in test data
            Assert.All(result, v => Assert.Equal("noun", v.TypeOfWord, ignoreCase: true));
        }

        [Fact]
        public async Task GetByTypeAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result1 = await _repository.GetByTypeAsync("noun");
            var result2 = await _repository.GetByTypeAsync("NOUN");
            var result3 = await _repository.GetByTypeAsync("Noun");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result1.Count, result3.Count);
        }

        [Fact]
        public async Task GetByTypeAsync_WithVerbType_ShouldReturnOnlyVerbs()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("verb");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("verb", result[0].TypeOfWord, ignoreCase: true);
            Assert.Equal("Run", result[0].Word);
        }

        [Fact]
        public async Task GetByTypeAsync_WithAdjectiveType_ShouldReturnOnlyAdjectives()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("adjective");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("adjective", result[0].TypeOfWord, ignoreCase: true);
            Assert.Equal("Beautiful", result[0].Word);
        }

        [Fact]
        public async Task GetByTypeAsync_WithNonExistentType_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("adverb");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByTypeAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 6 is deleted and is a noun, should not be included

            // Act
            var result = await _repository.GetByTypeAsync("noun");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 6);
            Assert.All(result, v => Assert.NotEqual(true, v.IsDeleted));
        }

        [Fact]
        public async Task GetByTypeAsync_ShouldOrderByWord()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("noun");

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
        public async Task GetByTypeAsync_WithNullType_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // No vocabularies have null TypeOfWord
        }

        [Fact]
        public async Task GetByTypeAsync_WithEmptyType_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByTypeAsync("");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByTypeAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetByTypeAsync("noun");

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

