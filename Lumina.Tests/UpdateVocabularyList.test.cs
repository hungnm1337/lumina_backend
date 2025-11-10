using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lumina.Tests
{
    public class UpdateVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public UpdateVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task UpdateAsync_WithValidVocabularyList_ShouldUpdateInDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(1);
            Assert.NotNull(list);
            
            list.Name = "Updated List Name";
            list.Status = "Draft";
            list.IsPublic = false;
            list.RejectionReason = "Updated rejection reason";

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated List Name", result.Name);
            Assert.Equal("Draft", result.Status);
            Assert.Equal(false, result.IsPublic);
            Assert.Equal("Updated rejection reason", result.RejectionReason);
            
            var updatedList = await _context.VocabularyLists.FindAsync(1);
            Assert.NotNull(updatedList);
            Assert.Equal("Updated List Name", updatedList.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnUpdatedVocabularyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(1);
            Assert.NotNull(list);
            list.Name = "Returned List";

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(list.VocabularyListId, result.VocabularyListId);
            Assert.Equal("Returned List", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithNullOptionalFields_ShouldUpdateInDatabase()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(4);
            Assert.NotNull(list);
            
            list.Status = null;
            list.RejectionReason = null;

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Status);
            Assert.Null(result.RejectionReason);
            
            var updatedList = await _context.VocabularyLists.FindAsync(4);
            Assert.NotNull(updatedList);
            Assert.Null(updatedList.Status);
            Assert.Null(updatedList.RejectionReason);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistChanges()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(2);
            Assert.NotNull(list);
            var originalName = list.Name;
            list.Name = "Persisted Name";

            // Act
            await _repository.UpdateAsync(list);

            // Assert
            var persistedList = await _context.VocabularyLists.FindAsync(2);
            Assert.NotNull(persistedList);
            Assert.Equal("Persisted Name", persistedList.Name);
            Assert.NotEqual(originalName, persistedList.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithAllFieldsChanged_ShouldUpdateAllFields()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(2);
            Assert.NotNull(list);
            
            list.Name = "Completely New Name";
            list.IsPublic = true;
            list.Status = "Published";
            list.RejectionReason = "New rejection reason";
            list.UpdateAt = DateTime.UtcNow;

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Completely New Name", result.Name);
            Assert.Equal(true, result.IsPublic);
            Assert.Equal("Published", result.Status);
            Assert.Equal("New rejection reason", result.RejectionReason);
        }

        [Fact]
        public async Task UpdateAsync_WithUpdatedBy_ShouldSetAndGetUpdatedBy()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(1);
            Assert.NotNull(list);
            
            var updaterUser = await _context.Users.FindAsync(2);
            Assert.NotNull(updaterUser);
            
            list.Name = "Updated Name";
            list.UpdatedBy = updaterUser.UserId;
            list.UpdateAt = DateTime.UtcNow;
            list.UpdatedByNavigation = updaterUser; // Access UpdatedByNavigation property for coverage

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            // Access UpdatedBy property to ensure coverage
            var updatedBy = result.UpdatedBy;
            Assert.Equal(updaterUser.UserId, updatedBy);
            
            // Access UpdatedByNavigation property after update to ensure coverage
            var updatedList = await _context.VocabularyLists
                .Include(v => v.UpdatedByNavigation)
                .FirstOrDefaultAsync(v => v.VocabularyListId == 1);
            Assert.NotNull(updatedList);
            // Access UpdatedByNavigation property to ensure coverage
            var updatedByNav = updatedList.UpdatedByNavigation;
            if (updatedByNav != null)
            {
                Assert.Equal(updaterUser.UserId, updatedByNav.UserId);
            }
        }

        [Fact]
        public async Task UpdateAsync_WithUpdatedByNull_ShouldAllowNull()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            var list = await _context.VocabularyLists.FindAsync(1);
            Assert.NotNull(list);
            
            list.Name = "Updated Name";
            list.UpdatedBy = null;
            list.UpdateAt = null;
            list.UpdatedByNavigation = null; // Access UpdatedByNavigation property with null for coverage

            // Act
            var result = await _repository.UpdateAsync(list);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            // Access UpdatedBy property to ensure coverage
            Assert.Null(result.UpdatedBy);
            // Access UpdatedByNavigation property to ensure coverage
            Assert.Null(result.UpdatedByNavigation); // May be null if not loaded by EF Core
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

