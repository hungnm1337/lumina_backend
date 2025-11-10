using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class DeleteVocabularyRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public DeleteVocabularyRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldSoftDeleteVocabulary()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            Assert.NotEqual(true, vocabulary.IsDeleted);

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            var deletedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(deletedVocabulary);
            Assert.Equal(true, deletedVocabulary.IsDeleted);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act & Assert
            await _repository.DeleteAsync(999);
            // Should not throw exception
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotRemoveFromDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabularyBeforeDelete = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabularyBeforeDelete);

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            var vocabularyAfterDelete = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabularyAfterDelete); // Still exists in database
            Assert.Equal(true, vocabularyAfterDelete.IsDeleted); // But marked as deleted
        }

        [Fact]
        public async Task DeleteAsync_WithAlreadyDeletedVocabulary_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(6); // Already deleted
            Assert.NotNull(vocabulary);
            Assert.Equal(true, vocabulary.IsDeleted);

            // Act & Assert
            await _repository.DeleteAsync(6);
            // Should not throw exception
            var stillDeleted = await _context.Vocabularies.FindAsync(6);
            Assert.NotNull(stillDeleted);
            Assert.Equal(true, stillDeleted.IsDeleted);
        }

        [Fact]
        public async Task DeleteAsync_ShouldPreserveOtherFields()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            var originalWord = vocabulary.Word;
            var originalDefinition = vocabulary.Definition;

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            var deletedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(deletedVocabulary);
            Assert.Equal(true, deletedVocabulary.IsDeleted);
            Assert.Equal(originalWord, deletedVocabulary.Word);
            Assert.Equal(originalDefinition, deletedVocabulary.Definition);
        }

        [Fact]
        public async Task DeleteAsync_WithZeroId_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act & Assert
            await _repository.DeleteAsync(0);
            // Should not throw exception
        }

        [Fact]
        public async Task DeleteAsync_WithNegativeId_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act & Assert
            await _repository.DeleteAsync(-1);
            // Should not throw exception
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

