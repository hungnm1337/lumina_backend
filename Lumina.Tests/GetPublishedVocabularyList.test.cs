using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetPublishedVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public GetPublishedVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task GetPublishedAsync_WithoutSearchTerm_ShouldReturnPublishedPublicLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.Equal("Published", list.Status);
                Assert.Equal(true, list.IsPublic);
                Assert.Equal(false, list.IsDeleted);
            });
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldExcludeDeletedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 5 is deleted

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 5);
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldExcludeDraftLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 2 is Draft

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 2);
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldExcludeRejectedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 4 is Rejected

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 4);
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldExcludePrivateLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 3 is Published but Private (IsPublic = false)

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 3);
        }

        [Fact]
        public async Task GetPublishedAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync("Published");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => 
            {
                Assert.Contains("Published", r.Name);
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.Equal("Published", list.Status);
                Assert.Equal(true, list.IsPublic);
            });
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldReturnVocabularyListDTOs()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.NotEmpty(resultList);
            var firstItem = resultList.First();
            Assert.IsType<VocabularyListDTO>(firstItem);
            Assert.True(firstItem.VocabularyListId > 0);
            Assert.NotEmpty(firstItem.Name);
            Assert.True(firstItem.IsPublic == true);
            Assert.Equal("Published", firstItem.Status);
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldIncludeVocabularyCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            var list1 = resultList.FirstOrDefault(r => r.VocabularyListId == 1);
            Assert.NotNull(list1);
            Assert.True(list1.VocabularyCount >= 0);
        }

        [Fact]
        public async Task GetPublishedAsync_ShouldOrderByCreateAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            if (resultList.Count > 1)
            {
                for (int i = 0; i < resultList.Count - 1; i++)
                {
                    Assert.True(resultList[i].CreateAt >= resultList[i + 1].CreateAt,
                        $"Lists should be ordered by CreateAt descending");
                }
            }
        }

        [Fact]
        public async Task GetPublishedAsync_WithEmptySearchTerm_ShouldReturnAllPublishedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result1 = await _repository.GetPublishedAsync(null);
            var result2 = await _repository.GetPublishedAsync("");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Count(), result2.Count());
        }

        [Fact]
        public async Task GetPublishedAsync_WithWhitespaceSearchTerm_ShouldReturnAllPublishedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync("   ");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
        }

        [Fact]
        public async Task GetPublishedAsync_WithNonExistentSearchTerm_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetPublishedAsync("NonExistentListName");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPublishedAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetPublishedAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

