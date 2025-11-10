using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByListAdditionalTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetByListAdditionalTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermAndNullCategory_ShouldNotIncludeNullCategoryInSearch()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // VocabularyId 5 has null Category and Word "Test"
            // When searching for something that would match if Category existed, it should not match

            // Act
            var result = await _repository.GetByListAsync(null, "Test");
            // This should find "Test" by Word, not by Category (since Category is null)

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test", result[0].Word);
            Assert.Null(result[0].Category);
        }

        [Fact]
        public async Task GetByListAsync_WithSearchTermMatchingNullCategory_ShouldOnlyMatchByWordOrDefinition()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            // Add a vocabulary with null category and a specific word
            var vocabWithNullCategory = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NullCategoryWord",
                Definition = "Definition for null category",
                TypeOfWord = "noun",
                Category = null,
                Example = null,
                IsDeleted = false
            };
            _context.Vocabularies.Add(vocabWithNullCategory);
            await _context.SaveChangesAsync();

            // Act - Search for something that won't match Word or Definition but might match Category if it existed
            var result = await _repository.GetByListAsync(null, "nonexistentcategory");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Should not match because Category is null
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

