using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class FindByIdVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public FindByIdVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task FindByIdAsync_WithValidId_ShouldReturnVocabularyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VocabularyListId);
            Assert.Equal("Published Public List", result.Name);
            Assert.Equal(true, result.IsPublic);
            Assert.Equal("Published", result.Status);
        }

        [Fact]
        public async Task FindByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindByIdAsync_WithDeletedVocabularyList_ShouldReturnVocabularyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // VocabularyListId 5 is deleted but FindByIdAsync doesn't filter by IsDeleted

            // Act
            var result = await _repository.FindByIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.VocabularyListId);
            Assert.Equal(true, result.IsDeleted);
        }

        [Fact]
        public async Task FindByIdAsync_ShouldReturnCompleteVocabularyListData()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VocabularyListId);
            Assert.Equal("Published Public List", result.Name);
            Assert.Equal(1, result.MakeBy);
            Assert.Equal(true, result.IsPublic);
            Assert.Equal("Published", result.Status);
            Assert.Null(result.RejectionReason);
            Assert.Equal(false, result.IsDeleted);
        }

        [Fact]
        public async Task FindByIdAsync_WithRejectedList_ShouldReturnVocabularyListWithRejectionReason()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.VocabularyListId);
            Assert.Equal("Rejected", result.Status);
            Assert.Equal("Inappropriate content", result.RejectionReason);
        }

        [Fact]
        public async Task FindByIdAsync_WithZeroId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindByIdAsync_WithNegativeId_ShouldReturnNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.FindByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindByIdAsync_WithEmptyDatabase_ShouldReturnNull()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.FindByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

