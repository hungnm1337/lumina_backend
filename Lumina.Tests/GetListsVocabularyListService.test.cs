using DataLayer.DTOs.Vocabulary;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class GetListsVocabularyListServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public GetListsVocabularyListServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetListsAsync_WithNullSearchTerm_ShouldReturnAllLists()
        {
            // Arrange
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "List 1" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "List 2" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetAllAsync(null))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetListsAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            _mockVocabularyListRepository.Verify(r => r.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetListsAsync_WithSearchTerm_ShouldReturnFilteredLists()
        {
            // Arrange
            var searchTerm = "Test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetAllAsync(searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetListsAsync(searchTerm);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetAllAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetListsByUserAsync_WithValidUserId_ShouldReturnUserLists()
        {
            // Arrange
            var userId = 1;
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "User List 1" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetByUserAsync(userId, null))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetListsByUserAsync(userId, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetByUserAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetListsByUserAsync_WithSearchTerm_ShouldReturnFilteredLists()
        {
            // Arrange
            var userId = 1;
            var searchTerm = "Test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetByUserAsync(userId, searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetListsByUserAsync(userId, searchTerm);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetByUserAsync(userId, searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetPublishedListsAsync_WithNullSearchTerm_ShouldReturnPublishedLists()
        {
            // Arrange
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Published List", Status = "Published" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetPublishedAsync(null))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetPublishedListsAsync(null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetPublishedAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetPublishedListsAsync_WithSearchTerm_ShouldReturnFilteredPublishedLists()
        {
            // Arrange
            var searchTerm = "Test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test Published List", Status = "Published" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetPublishedAsync(searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetPublishedListsAsync(searchTerm);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetPublishedAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithValidUserId_ShouldReturnLists()
        {
            // Arrange
            var userId = 1;
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "My List" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "Staff List" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetMyAndStaffListsAsync(userId, null))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetMyAndStaffListsAsync(userId, null);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            _mockVocabularyListRepository.Verify(r => r.GetMyAndStaffListsAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyAndStaffListsAsync_WithSearchTerm_ShouldReturnFilteredLists()
        {
            // Arrange
            var userId = 1;
            var searchTerm = "Test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            _mockVocabularyListRepository
                .Setup(r => r.GetMyAndStaffListsAsync(userId, searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _service.GetMyAndStaffListsAsync(userId, searchTerm);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            _mockVocabularyListRepository.Verify(r => r.GetMyAndStaffListsAsync(userId, searchTerm), Times.Once);
        }
    }
}

