using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class CreateListAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly Mock<RepositoryLayer.User.IUserRepository> _mockUserRepository;
        private readonly VocabularyListService _service;

        public CreateListAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUserRepository = new Mock<RepositoryLayer.User.IUserRepository>();

            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task CreateListAsync_WhenCreatorNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = false
            };

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.CreateListAsync(dto, 1)
            );

            Assert.Equal("Creator user not found.", exception.Message);
        }

        [Fact]
        public async Task CreateListAsync_WhenInputIsValid_ShouldCreateAndReturnDTO()
        {
            // Arrange
            var dto = new VocabularyListCreateDTO
            {
                Name = "Test List",
                IsPublic = true
            };

            var creator = new User
            {
                UserId = 1,
                FullName = "Test User",
                RoleId = 2
            };

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(creator);

            VocabularyList? capturedList = null;
            _mockVocabularyListRepository
                .Setup(repo => repo.AddAsync(It.IsAny<VocabularyList>()))
                .Callback<VocabularyList>(list => capturedList = list)
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateListAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test List", result.Name);
            Assert.True(result.IsPublic);
            Assert.Equal("Test User", result.MakeByName);
            Assert.Equal(2, result.MakeByRoleId);
            Assert.Equal("Draft", result.Status);
            Assert.Equal(0, result.VocabularyCount);

            Assert.NotNull(capturedList);
            Assert.Equal("Test List", capturedList.Name);
            Assert.True(capturedList.IsPublic);
            Assert.Equal(1, capturedList.MakeBy);
            Assert.False(capturedList.IsDeleted);
            Assert.Equal("Draft", capturedList.Status);
        }
    }
}













