using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class AddVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public AddVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task AddAsync_WithValidVocabularyList_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newList = new VocabularyList
            {
                Name = "New List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Draft"
            };

            // Act
            await _repository.AddAsync(newList);
            await _context.SaveChangesAsync();

            // Assert
            var savedList = await _context.VocabularyLists.FindAsync(newList.VocabularyListId);
            Assert.NotNull(savedList);
            Assert.Equal("New List", savedList.Name);
            Assert.Equal(1, savedList.MakeBy);
            Assert.Equal(true, savedList.IsPublic);
        }

        [Fact]
        public async Task AddAsync_WithMinimalFields_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newList = new VocabularyList
            {
                Name = "Minimal List",
                MakeBy = 2,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = false,
                Status = "Draft"
            };

            // Act
            await _repository.AddAsync(newList);
            await _context.SaveChangesAsync();

            // Assert
            var savedList = await _context.VocabularyLists
                .FirstOrDefaultAsync(vl => vl.Name == "Minimal List");
            Assert.NotNull(savedList);
            Assert.Equal("Minimal List", savedList.Name);
            Assert.Equal(2, savedList.MakeBy);
            Assert.Null(savedList.RejectionReason);
        }

        [Fact]
        public async Task AddAsync_WithNullOptionalFields_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newList = new VocabularyList
            {
                Name = "Null Fields List",
                MakeBy = 2,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = false,
                Status = null,
                RejectionReason = null
            };

            // Act
            await _repository.AddAsync(newList);
            await _context.SaveChangesAsync();

            // Assert
            var savedList = await _context.VocabularyLists
                .FirstOrDefaultAsync(vl => vl.Name == "Null Fields List");
            Assert.NotNull(savedList);
            Assert.Null(savedList.Status);
            Assert.Null(savedList.RejectionReason);
        }

        [Fact]
        public async Task AddAsync_WithRejectionReason_ShouldAddToDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newList = new VocabularyList
            {
                Name = "New Rejected List",
                MakeBy = 2,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Rejected",
                RejectionReason = "Test rejection reason"
            };

            // Act
            await _repository.AddAsync(newList);
            await _context.SaveChangesAsync();

            // Assert
            var savedList = await _context.VocabularyLists
                .FirstOrDefaultAsync(vl => vl.Name == "New Rejected List");
            Assert.NotNull(savedList);
            Assert.Equal("Rejected", savedList.Status);
            Assert.Equal("Test rejection reason", savedList.RejectionReason);
        }

        [Fact]
        public async Task AddAsync_ShouldNotAutoSetIsDeleted()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var newList = new VocabularyList
            {
                Name = "Not Deleted List",
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true,
                Status = "Published"
            };

            // Act
            await _repository.AddAsync(newList);
            await _context.SaveChangesAsync();

            // Assert
            var savedList = await _context.VocabularyLists
                .FirstOrDefaultAsync(vl => vl.Name == "Not Deleted List");
            Assert.NotNull(savedList);
            Assert.Equal(false, savedList.IsDeleted);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

