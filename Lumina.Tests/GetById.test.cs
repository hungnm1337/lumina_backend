using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByIdTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyRepository _repository;

        public GetByIdTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnVocabulary()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VocabularyId);
            Assert.Equal("Hello", result.Word);
            Assert.Equal("Xin chào", result.Definition);
            Assert.Equal("noun", result.TypeOfWord);
            Assert.Equal("greeting", result.Category);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithDeletedVocabulary_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(6); // VocabularyId 6 is deleted

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCompleteVocabularyData()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VocabularyId);
            Assert.Equal(1, result.VocabularyListId);
            Assert.Equal("Hello", result.Word);
            Assert.Equal("Xin chào", result.Definition);
            Assert.Equal("Hello, how are you?", result.Example);
            Assert.Equal("noun", result.TypeOfWord);
            Assert.Equal("greeting", result.Category);
            Assert.Equal(false, result.IsDeleted);
        }

        [Fact]
        public async Task GetByIdAsync_WithVocabularyHavingNullOptionalFields_ShouldReturnVocabulary()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(5); // VocabularyId 5 has null Category and Example

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.VocabularyId);
            Assert.Null(result.Category);
            Assert.Null(result.Example);
        }

        [Fact]
        public async Task GetByIdAsync_WithZeroId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithNegativeId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyDataAsync(_context);

            // Act
            var result = await _repository.GetByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyDatabase_ShouldReturnNull()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

