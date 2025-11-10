using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Vocabulary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class CreateListVocabularyListServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly VocabularyListService _service;

        public CreateListVocabularyListServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task CreateListAsync_WithValidData_ShouldCreateNewList()
        {
            // Arrange
            var creatorUserId = 1;
            var creator = new DataLayer.Models.User
            {
                UserId = creatorUserId,
                FullName = "Test User",
                Email = "test@example.com"
            };
            var dto = new VocabularyListCreateDTO
            {
                Name = "My Vocabulary List",
                IsPublic = true
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(creatorUserId))
                .ReturnsAsync(creator);
            _mockVocabularyListRepository
                .Setup(r => r.AddAsync(It.IsAny<VocabularyList>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateListAsync(dto, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("My Vocabulary List", result.Name);
            Assert.True(result.IsPublic);
            Assert.Equal("Test User", result.MakeByName);
            Assert.Equal("Draft", result.Status);
            Assert.Equal(0, result.VocabularyCount);
            Assert.Null(result.RejectionReason);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(creatorUserId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.AddAsync(It.IsAny<VocabularyList>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateListAsync_WithIsPublicFalse_ShouldSetIsPublicFalse()
        {
            // Arrange
            var creatorUserId = 1;
            var creator = new DataLayer.Models.User
            {
                UserId = creatorUserId,
                FullName = "Test User"
            };
            var dto = new VocabularyListCreateDTO
            {
                Name = "Private List",
                IsPublic = false
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(creatorUserId))
                .ReturnsAsync(creator);
            _mockVocabularyListRepository
                .Setup(r => r.AddAsync(It.IsAny<VocabularyList>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateListAsync(dto, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsPublic);
        }

        [Fact]
        public async Task CreateListAsync_WithNonExistentCreator_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var creatorUserId = 999;
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = false
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(creatorUserId))
                .ReturnsAsync((DataLayer.Models.User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _service.CreateListAsync(dto, creatorUserId);
            });

            Assert.Equal("Creator user not found.", exception.Message);
            _mockVocabularyListRepository.Verify(r => r.AddAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task CreateListAsync_ShouldSetStatusToDraft()
        {
            // Arrange
            var creatorUserId = 1;
            var creator = new DataLayer.Models.User
            {
                UserId = creatorUserId,
                FullName = "Test User"
            };
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = true
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(creatorUserId))
                .ReturnsAsync(creator);
            _mockVocabularyListRepository
                .Setup(r => r.AddAsync(It.IsAny<VocabularyList>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateListAsync(dto, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Draft", result.Status);
        }

        [Fact]
        public async Task CreateListAsync_ShouldSetIsDeletedToFalse()
        {
            // Arrange
            var creatorUserId = 1;
            var creator = new DataLayer.Models.User
            {
                UserId = creatorUserId,
                FullName = "Test User"
            };
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = false
            };

            VocabularyList? capturedList = null;
            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(creatorUserId))
                .ReturnsAsync(creator);
            _mockVocabularyListRepository
                .Setup(r => r.AddAsync(It.IsAny<VocabularyList>()))
                .Callback<VocabularyList>(list => capturedList = list)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateListAsync(dto, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(capturedList);
            Assert.False(capturedList.IsDeleted);
        }
    }
}

