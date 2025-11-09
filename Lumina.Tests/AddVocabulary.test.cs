using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class AddVocabularyTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public AddVocabularyTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task AddAsync_WithValidVocabulary_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NewWord",
                Definition = "New Definition",
                TypeOfWord = "noun",
                Category = "test",
                Example = "New example",
                IsDeleted = false
            };

            // Act
            await _repository.AddAsync(newVocabulary);
            await _context.SaveChangesAsync();

            // Assert
            var savedVocabulary = await _context.Vocabularies.FindAsync(newVocabulary.VocabularyId);
            Assert.NotNull(savedVocabulary);
            Assert.Equal("NewWord", savedVocabulary.Word);
            Assert.Equal("New Definition", savedVocabulary.Definition);
        }

        [Fact]
        public async Task AddAsync_WithMinimalRequiredFields_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "Minimal",
                Definition = "Minimal definition",
                TypeOfWord = "noun",
                IsDeleted = false
            };

            // Act
            await _repository.AddAsync(newVocabulary);
            await _context.SaveChangesAsync();

            // Assert
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "Minimal");
            Assert.NotNull(savedVocabulary);
            Assert.Equal("Minimal", savedVocabulary.Word);
            Assert.Null(savedVocabulary.Category);
            Assert.Null(savedVocabulary.Example);
        }

        [Fact]
        public async Task AddAsync_WithNullOptionalFields_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NullFields",
                Definition = "Definition",
                TypeOfWord = "noun",
                Category = null,
                Example = null,
                IsDeleted = false
            };

            // Act
            await _repository.AddAsync(newVocabulary);
            await _context.SaveChangesAsync();

            // Assert
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "NullFields");
            Assert.NotNull(savedVocabulary);
            Assert.Null(savedVocabulary.Category);
            Assert.Null(savedVocabulary.Example);
        }

        [Fact]
        public async Task AddAsync_WithDifferentVocabularyListId_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 2,
                Word = "List2Word",
                Definition = "Definition",
                TypeOfWord = "verb",
                IsDeleted = false
            };

            // Act
            await _repository.AddAsync(newVocabulary);
            await _context.SaveChangesAsync();

            // Assert
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "List2Word");
            Assert.NotNull(savedVocabulary);
            Assert.Equal(2, savedVocabulary.VocabularyListId);
        }

        [Fact]
        public async Task AddAsync_ShouldNotAutoSetIsDeleted()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);
            var newVocabulary = new Vocabulary
            {
                VocabularyListId = 1,
                Word = "NotDeleted",
                Definition = "Definition",
                TypeOfWord = "noun",
                IsDeleted = false
            };

            // Act
            await _repository.AddAsync(newVocabulary);
            await _context.SaveChangesAsync();

            // Assert
            var savedVocabulary = await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Word == "NotDeleted");
            Assert.NotNull(savedVocabulary);
            Assert.Equal(false, savedVocabulary.IsDeleted);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

