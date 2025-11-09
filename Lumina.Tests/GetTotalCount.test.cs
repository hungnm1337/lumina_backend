using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetTotalCountTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetTotalCountTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetTotalCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(5, result); // 5 vocabularies (excluding deleted)
        }

        [Fact]
        public async Task GetTotalCountAsync_ShouldExcludeDeletedVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 6 is deleted and should not be counted

            // Act
            var result = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(5, result); // Should not count deleted vocabulary
        }

        [Fact]
        public async Task GetTotalCountAsync_WithEmptyDatabase_ShouldReturnZero()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetTotalCountAsync_AfterAddingVocabulary_ShouldIncreaseCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var initialCount = await _repository.GetTotalCountAsync();
            
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NewWord",
                Definition = "New Definition",
                TypeOfWord = "noun",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            // Act
            var newCount = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(initialCount + 1, newCount);
        }

        [Fact]
        public async Task GetTotalCountAsync_AfterDeletingVocabulary_ShouldDecreaseCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var initialCount = await _repository.GetTotalCountAsync();
            
            await _repository.DeleteAsync(1);

            // Act
            var newCount = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(initialCount - 1, newCount);
        }

        [Fact]
        public async Task GetTotalCountAsync_ShouldReturnOnlyActiveVocabularies()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add a deleted vocabulary
            _context.Vocabularies.Add(new Vocabulary
            {
                VocabularyListId = 1,
                Word = "DeletedWord",
                Definition = "Deleted Definition",
                TypeOfWord = "noun",
                IsDeleted = true
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetTotalCountAsync();

            // Assert
            Assert.Equal(5, result); // Should not count deleted vocabulary
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

