using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetByUserVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public GetByUserVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task GetByUserAsync_WithValidUserId_ShouldReturnUserLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.Equal(2, list.MakeBy);
            });
        }

        [Fact]
        public async Task GetByUserAsync_ShouldExcludeDeletedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 5 is deleted and belongs to user 2

            // Act
            var result = await _repository.GetByUserAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 5);
        }

        [Fact]
        public async Task GetByUserAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, "Draft");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => 
            {
                Assert.Contains("Draft", r.Name);
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.Equal(2, list.MakeBy);
            });
        }

        [Fact]
        public async Task GetByUserAsync_ShouldReturnOnlyUserLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(1, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.All(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.Equal(1, list.MakeBy);
            });
        }

        [Fact]
        public async Task GetByUserAsync_ShouldReturnVocabularyListDTOs()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.NotEmpty(resultList);
            var firstItem = resultList.First();
            Assert.IsType<VocabularyListDTO>(firstItem);
            Assert.True(firstItem.VocabularyListId > 0);
            Assert.NotEmpty(firstItem.Name);
        }

        [Fact]
        public async Task GetByUserAsync_ShouldIncludeVocabularyCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            var list2 = resultList.FirstOrDefault(r => r.VocabularyListId == 2);
            Assert.NotNull(list2);
            Assert.True(list2.VocabularyCount >= 0);
        }

        [Fact]
        public async Task GetByUserAsync_ShouldOrderByCreateAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, null);

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
        public async Task GetByUserAsync_WithNonExistentUserId_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(999, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserAsync_WithEmptySearchTerm_ShouldReturnAllUserLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result1 = await _repository.GetByUserAsync(2, null);
            var result2 = await _repository.GetByUserAsync(2, "");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Count(), result2.Count());
        }

        [Fact]
        public async Task GetByUserAsync_WithWhitespaceSearchTerm_ShouldReturnAllUserLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, "   ");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
        }

        [Fact]
        public async Task GetByUserAsync_WithNonExistentSearchTerm_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetByUserAsync(2, "NonExistentListName");

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

