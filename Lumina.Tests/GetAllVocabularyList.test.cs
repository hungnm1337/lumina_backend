using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetAllVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public GetAllVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task GetAllAsync_WithoutSearchTerm_ShouldReturnAllNonDeletedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(6, resultList.Count); // 6 non-deleted lists (excluding list 5)
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 5); // Deleted list
        }

        [Fact]
        public async Task GetAllAsync_ShouldExcludeDeletedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 5 is deleted

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 5);
            Assert.All(resultList, r => Assert.NotEqual(true, _context.VocabularyLists.Find(r.VocabularyListId)?.IsDeleted));
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync("Published");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => Assert.Contains("Published", r.Name));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnVocabularyListDTOs()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.NotEmpty(resultList);
            var firstItem = resultList.First();
            Assert.IsType<VocabularyListDTO>(firstItem);
            Assert.True(firstItem.VocabularyListId > 0);
            Assert.NotEmpty(firstItem.Name);
            Assert.NotEmpty(firstItem.MakeByName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludeVocabularyCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            var list1 = resultList.FirstOrDefault(r => r.VocabularyListId == 1);
            Assert.NotNull(list1);
            Assert.True(list1.VocabularyCount >= 1); // At least 1 non-deleted vocabulary
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludeMakeByName()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            var list1 = resultList.FirstOrDefault(r => r.VocabularyListId == 1);
            Assert.NotNull(list1);
            Assert.Equal("Staff User", list1.MakeByName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldOrderByCreateAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            if (resultList.Count > 1)
            {
                for (int i = 0; i < resultList.Count - 1; i++)
                {
                    Assert.True(resultList[i].CreateAt >= resultList[i + 1].CreateAt,
                        $"Lists should be ordered by CreateAt descending: {resultList[i].CreateAt} should be >= {resultList[i + 1].CreateAt}");
                }
            }
        }

        [Fact]
        public async Task GetAllAsync_WithEmptySearchTerm_ShouldReturnAllLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result1 = await _repository.GetAllAsync(null);
            var result2 = await _repository.GetAllAsync("");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Count(), result2.Count());
        }

        [Fact]
        public async Task GetAllAsync_WithWhitespaceSearchTerm_ShouldReturnAllLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync("   ");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Count()); // All non-deleted lists
        }

        [Fact]
        public async Task GetAllAsync_WithNonExistentSearchTerm_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync("NonExistentListName");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange - no seed data

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludeAllStatuses()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetAllAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Contains(resultList, r => r.Status == "Published");
            Assert.Contains(resultList, r => r.Status == "Draft");
            Assert.Contains(resultList, r => r.Status == "Rejected");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

