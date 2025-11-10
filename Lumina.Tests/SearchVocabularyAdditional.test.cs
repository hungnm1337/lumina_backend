using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class SearchVocabularyAdditionalTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public SearchVocabularyAdditionalTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task SearchAsync_WithVocabularyHavingNullExample_ShouldNotMatchExampleInSearch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Example
            // Add a vocabulary with null example and a word that could match example search
            var vocabWithNullExample = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NullExampleWord",
                Definition = "Definition",
                TypeOfWord = "noun",
                Category = "test",
                Example = null, // Null example
                IsDeleted = false
            };
            _context.Vocabularies.Add(vocabWithNullExample);
            await _context.SaveChangesAsync();

            // Act - Search for something that would match Example if it existed
            var result = await _repository.SearchAsync("examplecontentthatdoesnotexist");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == vocabWithNullExample.VocabularyId && v.Example == null);
        }

        [Fact]
        public async Task SearchAsync_WithVocabularyHavingNullExampleButMatchingWord_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var vocabWithNullExample = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NullExampleTest",
                Definition = "Definition",
                TypeOfWord = "noun",
                Category = "test",
                Example = null, // Null example
                IsDeleted = false
            };
            _context.Vocabularies.Add(vocabWithNullExample);
            await _context.SaveChangesAsync();

            // Act - Search for the word itself
            var result = await _repository.SearchAsync("NullExampleTest");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.VocabularyId == vocabWithNullExample.VocabularyId);
            Assert.Null(result.First(v => v.VocabularyId == vocabWithNullExample.VocabularyId).Example);
        }

        [Fact]
        public async Task SearchAsync_WithVocabularyHavingNullCategory_ShouldNotMatchCategoryInSearch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category
            // Search for something that would match Category if it existed

            // Act
            var result = await _repository.SearchAsync("nonexistentcategory");

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, v => v.VocabularyId == 5 && v.Category == null);
        }

        [Fact]
        public async Task SearchAsync_WithVocabularyHavingNullCategoryButMatchingWord_ShouldReturnResult()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category and Word "Test"

            // Act
            var result = await _repository.SearchAsync("Test");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, v => v.VocabularyId == 5);
            Assert.Null(result.First(v => v.VocabularyId == 5).Category);
        }

        [Fact]
        public async Task SearchAsync_WithExampleContainingSearchTerm_ShouldMatchExample()
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
            Assert.Contains("how are you", result.First(v => v.VocabularyId == 1).Example, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchAsync_WithTypeOfWordContainingSearchTerm_ShouldMatchTypeOfWord()
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
        public async Task SearchAsync_WithCategoryContainingSearchTerm_ShouldMatchCategory()
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
        public async Task SearchAsync_WithListIdAndSearchTerm_ShouldFilterByBoth()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync("noun", 1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, v => Assert.Equal(1, v.VocabularyListId));
            Assert.All(result, v => Assert.Contains("noun", v.TypeOfWord, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchAsync_WithListIdButNoSearchTerm_ShouldReturnAllInList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.SearchAsync(null, 1);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, v => Assert.Equal(1, v.VocabularyListId));
            Assert.True(result.Count >= 4); // At least 4 vocabularies in list 1 (excluding deleted)
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

