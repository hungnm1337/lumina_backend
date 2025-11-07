using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetCountsByListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetCountsByListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetCountsByListAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetCountsByListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey(1));
            Assert.True(result.ContainsKey(2));
            Assert.Equal(4, result[1]); // 4 vocabularies in list 1 (excluding deleted)
            Assert.Equal(1, result[2]); // 1 vocabulary in list 2
        }

        [Fact]
        public async Task GetCountsByListAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 6 is deleted and should not be counted

            // Act
            var result = await _repository.GetCountsByListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[1]); // Should not count deleted vocabulary
        }

        [Fact]
        public async Task GetCountsByListAsync_WithEmptyDatabase_ShouldReturnEmptyDictionary()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetCountsByListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCountsByListAsync_ShouldGroupByVocabularyListId()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add more vocabularies to list 1
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Additional",
                Definition = "Additional definition",
                TypeOfWord = "noun",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetCountsByListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey(1));
            Assert.Equal(5, result[1]); // Should include the new vocabulary
        }

        [Fact]
        public async Task GetCountsByListAsync_ShouldReturnCorrectCountForEachList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetCountsByListAsync();

            // Assert
            Assert.NotNull(result);
            var list1Count = result.GetValueOrDefault(1, 0);
            var list2Count = result.GetValueOrDefault(2, 0);
            Assert.Equal(4, list1Count);
            Assert.Equal(1, list2Count);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

