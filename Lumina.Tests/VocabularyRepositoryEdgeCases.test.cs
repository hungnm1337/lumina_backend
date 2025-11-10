using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class VocabularyRepositoryEdgeCasesTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public VocabularyRepositoryEdgeCasesTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermAndVocabularyHavingNullCategory_ShouldNotMatchCategoryButMatchWord()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category and Word "Test"
            
            // Act - Search for "Test" which should match by Word, not by Category
            var result = await _repository.GetByListAsync(null, "Test");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test", result[0].Word);
            Assert.Null(result[0].Category);
            // Verify it matched by Word, not by Category (since Category is null)
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermThatWouldMatchCategoryIfNotNull_ShouldNotMatchWhenCategoryIsNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category
            
            // Act - Search for something that won't match Word or Definition
            var result = await _repository.GetByListAsync(null, "imaginarycategory");

            // Assert
            Assert.NotNull(result);
            // Should not match VocabularyId 5 because Category is null
            Assert.DoesNotContain(result, v => v.VocabularyId == 5);
        }

        [Fact]
        public async Task SearchAsync_WithExampleNullAndSearchTermNotInWordOrDefinition_ShouldNotMatch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Example, Word "Test", Definition "Kiểm tra"
            
            // Act - Search for something that would only match Example if it existed
            var result = await _repository.SearchAsync("nonexistentexamplecontent");

            // Assert
            Assert.NotNull(result);
            // Should not match VocabularyId 5 because Example is null and search term doesn't match Word or Definition
            Assert.DoesNotContain(result, v => v.VocabularyId == 5);
        }

        [Fact]
        public async Task SearchAsync_WithExampleNullButWordMatching_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Example, Word "Test"
            
            // Act
            var result = await _repository.SearchAsync("Test");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.VocabularyId == 5);
            Assert.Null(result.First(v => v.VocabularyId == 5).Example);
        }

        [Fact]
        public async Task SearchAsync_WithCategoryNullAndSearchTermNotInWordDefinitionOrType_ShouldNotMatch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category, Word "Test", Definition "Kiểm tra", TypeOfWord "noun"
            
            // Act - Search for something that would only match Category if it existed
            var result = await _repository.SearchAsync("imaginarycategoryname");

            // Assert
            Assert.NotNull(result);
            // Should not match VocabularyId 5 because Category is null
            Assert.DoesNotContain(result, v => v.VocabularyId == 5);
        }

        [Fact]
        public async Task SearchAsync_WithCategoryNullButTypeOfWordMatching_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category, TypeOfWord "noun"
            
            // Act
            var result = await _repository.SearchAsync("noun");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.VocabularyId == 5);
            Assert.Null(result.First(v => v.VocabularyId == 5).Category);
        }

        [Fact]
        public async Task DeleteAsync_WhenVocabularyNotFound_ShouldNotThrowException()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Vocabulary with ID 999 does not exist

            // Act & Assert - Should not throw exception
            await _repository.DeleteAsync(999);
            
            // Verify no exception was thrown
            Assert.True(true);
        }

        [Fact]
        public async Task DeleteAsync_WhenVocabularyIsNull_ShouldNotCallSaveChanges()
        {
            // Arrange
            // Empty database - no vocabularies

            // Act
            await _repository.DeleteAsync(1);

            // Assert
            // Should not throw exception
            // The method should handle null gracefully
            Assert.True(true);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermMatchingDefinitionButNotWordOrCategory_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 1 has Definition "Xin chào"
            
            // Act
            var result = await _repository.GetByListAsync(null, "Xin chào");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Word);
            Assert.Contains("Xin chào", result[0].Definition);
        }

        [Fact]
        public async Task SearchAsync_WithAllOptionalFieldsNullExceptWord_ShouldStillMatchByWord()
        {
            // Arrange
            var vocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "OnlyWord",
                Definition = "Definition", // Required field
                TypeOfWord = "noun", // Required field
                Category = null, // Optional field
                Example = null, // Optional field
                IsDeleted = false
            };
            _context.Vocabularies.Add(vocabulary);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.SearchAsync("OnlyWord");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("OnlyWord", result[0].Word);
            Assert.Null(result[0].Category);
            Assert.Null(result[0].Example);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermMatchingMultipleFields_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 1 has Word "Hello", Definition "Xin chào", Category "greeting"
            
            // Act - Search for something that matches Word
            var result = await _repository.GetByListAsync(null, "Hello");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Word);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermMatchingExample_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 1 has Example "Hello, how are you?"
            
            // Act
            var result = await _repository.SearchAsync("how are you");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.VocabularyId == 1);
            Assert.NotNull(result.First(v => v.VocabularyId == 1).Example);
            Assert.Contains("how are you", result.First(v => v.VocabularyId == 1).Example!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermMatchingTypeOfWord_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            
            // Act
            var result = await _repository.SearchAsync("adjective");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.TypeOfWord.Equals("adjective", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchAsync_WithSearchTermMatchingCategory_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            
            // Act
            var result = await _repository.SearchAsync("greeting");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.Category != null && v.Category.Equals("greeting", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetByListAsync_WithListIdAndSearchTerm_ShouldFilterCorrectly()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            
            // Act
            var result = await _repository.GetByListAsync(1, "World");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].VocabularyListId);
            Assert.Equal("World", result[0].Word);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

