using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests
{
    public class GetMyAndStaffListsVocabularyListTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly VocabularyListRepository _repository;

        public GetMyAndStaffListsVocabularyListTests()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _repository = new VocabularyListRepository(_context);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithStudentUserId_ShouldReturnUserAndStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            // Should include lists made by user 2 and lists made by staff (user 1)
            Assert.Contains(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                return list != null && (list.MakeBy == 2 || list.MakeBy == 1);
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldExcludeDeletedLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 5 is deleted and belongs to user 2

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.DoesNotContain(resultList, r => r.VocabularyListId == 5);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldIncludeUserLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            // Should include lists made by user 2
            Assert.Contains(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                return list != null && list.MakeBy == 2;
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldIncludeStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            // Should include lists made by staff (user 1 with RoleId 3)
            Assert.Contains(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                if (list != null)
                {
                    var user = _context.Users.Find(list.MakeBy);
                    return user != null && user.RoleId == 3;
                }
                return false;
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldExcludeOtherStudentLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);
            // List 7 is made by user 3 (student, not user 2)

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            // Should not include lists made by other students
            Assert.DoesNotContain(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                if (list != null && list.MakeBy == 3)
                {
                    var user = _context.Users.Find(3);
                    return user != null && user.RoleId != 3; // Not staff
                }
                return false;
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, "Published");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
            Assert.All(resultList, r => 
            {
                Assert.Contains("Published", r.Name);
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                Assert.NotNull(list);
                Assert.True(list.MakeBy == 2 || list.MakeBy == 1);
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldReturnVocabularyListDTOs()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

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
        public async Task GetMyAndStaffListsAsync_ShouldIncludeVocabularyCount()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.All(resultList, r => Assert.True(r.VocabularyCount >= 0));
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_ShouldOrderByCreateAtDescending()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, null);

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
        public async Task GetMyAndStaffListsAsync_WithStaffUserId_ShouldReturnStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(1, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            // Should include lists made by staff (user 1)
            Assert.Contains(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                return list != null && list.MakeBy == 1;
            });
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithEmptySearchTerm_ShouldReturnAllUserAndStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result1 = await _repository.GetMyAndStaffListsAsync(2, null);
            var result2 = await _repository.GetMyAndStaffListsAsync(2, "");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Count(), result2.Count());
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithWhitespaceSearchTerm_ShouldReturnAllUserAndStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, "   ");

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.True(resultList.Count > 0);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithNonExistentSearchTerm_ShouldReturnEmptyList()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(2, "NonExistentListName");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithNonExistentUserId_ShouldReturnOnlyStaffLists()
        {
            // Arrange
            await InMemoryDbContextHelper.SeedVocabularyListDataAsync(_context);

            // Act
            var result = await _repository.GetMyAndStaffListsAsync(999, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            // Should only return staff lists
            Assert.All(resultList, r => 
            {
                var list = _context.VocabularyLists.Find(r.VocabularyListId);
                if (list != null)
                {
                    var user = _context.Users.Find(list.MakeBy);
                    Assert.NotNull(user);
                    Assert.Equal(3, user.RoleId); // Staff role
                }
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

