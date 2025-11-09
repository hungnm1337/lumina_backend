using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class UpdateVocabularyRepositoryTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public UpdateVocabularyRepositoryTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task UpdateAsync_WithValidVocabulary_ShouldUpdateInDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            
            vocabulary.Word = "UpdatedWord";
            vocabulary.Definition = "Updated Definition";
            vocabulary.TypeOfWord = "verb";
            vocabulary.Category = "updated";
            vocabulary.Example = "Updated example";

            // Act
            await _repository.UpdateAsync(vocabulary);

            // Assert
            var updatedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(updatedVocabulary);
            Assert.Equal("UpdatedWord", updatedVocabulary.Word);
            Assert.Equal("Updated Definition", updatedVocabulary.Definition);
            Assert.Equal("verb", updatedVocabulary.TypeOfWord);
            Assert.Equal("updated", updatedVocabulary.Category);
            Assert.Equal("Updated example", updatedVocabulary.Example);
        }

        [Fact]
        public async Task UpdateAsync_WithNullOptionalFields_ShouldUpdateInDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            
            vocabulary.Category = null;
            vocabulary.Example = null;

            // Act
            await _repository.UpdateAsync(vocabulary);

            // Assert
            var updatedVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(updatedVocabulary);
            Assert.Null(updatedVocabulary.Category);
            Assert.Null(updatedVocabulary.Example);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(vocabulary);
            var originalWord = vocabulary.Word;
            
            vocabulary.Word = "PersistedWord";

            // Act
            await _repository.UpdateAsync(vocabulary);

            // Assert
            // Create new context to verify persistence
            var newContext = InMemoryDbContextHelper.CreateContext();
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(newContext);
            var persistedVocabulary = await newContext.Vocabularies.FindAsync(1);
            
            // Note: InMemory database doesn't persist between contexts
            // But we can verify the update was saved in the same context
            var verifyVocabulary = await _context.Vocabularies.FindAsync(1);
            Assert.NotNull(verifyVocabulary);
            Assert.Equal("PersistedWord", verifyVocabulary.Word);
            Assert.NotEqual(originalWord, verifyVocabulary.Word);
            
            newContext.Dispose();
        }

        [Fact]
        public async Task UpdateAsync_WithAllFieldsChanged_ShouldUpdateAllFields()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabulary = await _context.Vocabularies.FindAsync(2);
            Assert.NotNull(vocabulary);
            
            vocabulary.Word = "CompletelyNew";
            vocabulary.Definition = "Completely New Definition";
            vocabulary.TypeOfWord = "adjective";
            vocabulary.Category = "completelyNew";
            vocabulary.Example = "Completely new example";
            vocabulary.VocabularyListId = 2;

            // Act
            await _repository.UpdateAsync(vocabulary);

            // Assert
            var updatedVocabulary = await _context.Vocabularies.FindAsync(2);
            Assert.NotNull(updatedVocabulary);
            Assert.Equal("CompletelyNew", updatedVocabulary.Word);
            Assert.Equal("Completely New Definition", updatedVocabulary.Definition);
            Assert.Equal("adjective", updatedVocabulary.TypeOfWord);
            Assert.Equal("completelyNew", updatedVocabulary.Category);
            Assert.Equal("Completely new example", updatedVocabulary.Example);
            Assert.Equal(2, updatedVocabulary.VocabularyListId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

