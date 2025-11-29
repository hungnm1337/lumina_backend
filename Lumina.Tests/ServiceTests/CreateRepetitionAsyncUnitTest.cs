using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class CreateRepetitionAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly SpacedRepetitionService _service;

        public CreateRepetitionAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WhenInputIsValid_ShouldCreateAndReturnDTO()
        {
            // Arrange
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List"
            };

            _mockRepetitionRepository
                .Setup(repo => repo.ExistsAsync(1, 1))
                .ReturnsAsync(false);

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(vocabularyList);

            _mockRepetitionRepository
                .Setup(repo => repo.AddAsync(It.IsAny<UserSpacedRepetition>()))
                .ReturnsAsync((UserSpacedRepetition r) => r);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateRepetitionAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal(1, result.VocabularyListId);
            Assert.Equal("New", result.Status);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WhenAlreadyExists_ShouldReturnExisting()
        {
            // Arrange
            var existingItem = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = 1,
                VocabularyListId = 1,
                VocabularyList = new VocabularyList { Name = "Test List" }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.ExistsAsync(1, 1))
                .ReturnsAsync(true);

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserAndListAsync(1, 1))
                .ReturnsAsync(existingItem);

            // Act
            var result = await _service.CreateRepetitionAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserSpacedRepetitionId);
        }

        [Fact]
        public async Task CreateRepetitionAsync_WhenVocabularyListNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockRepetitionRepository
                .Setup(repo => repo.ExistsAsync(1, 1))
                .ReturnsAsync(false);

            _mockVocabularyListRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((VocabularyList?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.CreateRepetitionAsync(1, 1)
            );
        }
    }
}

